using AutoMapper;
using Common.DTOs.AppealDto;
using Common.Enums;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class AppealService : IAppealService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public AppealService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<AppealResponseDto> CreateAppealAsync(CreateAppealDto dto, int currentUserId)
        {
            // 1️ Check user is a team member
            var isMember = await _uow.TeamMembers.ExistsAsync(m => m.TeamId == dto.TeamId && m.UserId == currentUserId);
            if (!isMember)
                throw new Exception("You are not a member of this team.");

            // 2️ Validate & Force Null
            if (dto.AppealType == AppealType.Penalty)
            {
                if (!dto.AdjustmentId.HasValue)
                    throw new Exception("AdjustmentId is required for penalty appeals.");

                dto.ScoreId = null; // FORCE null for safety
            }
            else if (dto.AppealType == AppealType.Score)
            {
                if (!dto.ScoreId.HasValue)
                    throw new Exception("ScoreId is required for score appeals.");

                dto.AdjustmentId = null; // FORCE null for safety
            }
            else
            {
                throw new Exception("Invalid AppealType.");
            }

            // 3️ Validate referenced record
            if (dto.AppealType == AppealType.Penalty)
            {
                var adj = await _uow.PenaltiesBonuses.GetByIdAsync(dto.AdjustmentId!.Value);
                if (adj == null)
                    throw new Exception("Penalty/Bonus not found.");

                if (adj.TeamId != dto.TeamId)
                    throw new Exception("You cannot appeal adjustment belonging to another team.");
            }
            else
            {
                var score = await _uow.Scores.GetByIdAsync(dto.ScoreId!.Value);
                if (score == null)
                    throw new Exception("Score not found.");

                if (score.Submission.TeamId != dto.TeamId)
                    throw new Exception("You cannot appeal score belonging to another team.");
            }

            // 4️ Prevent duplicate appeals
            bool duplicateExists = await _uow.Appeals.ExistsAsync(a =>
               a.TeamId == dto.TeamId &&
               a.AppealType == dto.AppealType &&
               a.Status == AppealStatus.Pending &&
               ((dto.AdjustmentId.HasValue && a.AdjustmentId == dto.AdjustmentId) ||
                (dto.ScoreId.HasValue && a.ScoreId == dto.ScoreId))
           );

            if (duplicateExists)
                throw new InvalidOperationException("An active pending appeal already exists for this item.");

            // 5️ Create appeal
            var appeal = new Appeal
            {
                AppealType = dto.AppealType,
                AdjustmentId = dto.AdjustmentId,
                ScoreId = dto.ScoreId,
                TeamId = dto.TeamId,
                Message = dto.Message,
                Reason = dto.Reason,
                Status = AppealStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Appeals.AddAsync(appeal);
            await _uow.SaveAsync();

            // reload with includes for mapping
            var created = await _uow.Appeals.GetByIdIncludingAsync(a => a.AppealId == appeal.AppealId,
                a => a.Team,
                a => a.Score,
                a => a.ReviewedBy,
                a => a.Adjustment);
            return _mapper.Map<AppealResponseDto>(created);
        }

        public async Task<IEnumerable<AppealResponseDto>> GetAppealsByTeamAsync(int teamId)
        {
            var appeals = await _uow.Appeals.GetAllAsync(
                a => a.TeamId == teamId,
                includeProperties: "Team,Adjustment,Score,ReviewedBy"
            );

            return _mapper.Map<IEnumerable<AppealResponseDto>>(appeals);
        }

        public async Task<IEnumerable<AppealResponseDto>> GetAllAppealsAsync()
        {
            var appeals = await _uow.Appeals.GetAllAsync(includeProperties: "Team,Adjustment,Score,ReviewedBy");
            return _mapper.Map<IEnumerable<AppealResponseDto>>(appeals);
        }

        public async Task<AppealResponseDto?> GetAppealByIdAsync(int appealId)
        {
            var appeal = await _uow.Appeals.GetByIdIncludingAsync(
                a => a.AppealId == appealId,
                a => a.Team,
                a => a.Score,
                a => a.ReviewedBy,
                a => a.Adjustment);
            return appeal == null ? null : _mapper.Map<AppealResponseDto>(appeal);
        }

        public async Task<AppealResponseDto?> ReviewAppealAsync(int appealId, ReviewAppealDto dto, int reviewerUserId)
        {
            var appeal = await _uow.Appeals.GetByIdAsync(appealId);
            if (appeal == null)
                return null;

            // 1️ Only pending appeals can be reviewed
            if (appeal.Status != AppealStatus.Pending)
                throw new InvalidOperationException("This appeal has already been reviewed.");

            // 2️ Apply admin review
            appeal.Status = dto.Status;
            appeal.AdminResponse = dto.AdminResponse;
            appeal.ReviewedById = reviewerUserId;
            appeal.ReviewedAt = DateTime.UtcNow;

            // 3️ If approved → apply business logic
            if (dto.Status == AppealStatus.Approved)
            {
                if (appeal.AppealType == AppealType.Penalty && appeal.AdjustmentId.HasValue)
                {
                    // revert penalty (do not delete)
                    var penalty = await _uow.PenaltiesBonuses.GetByIdAsync(appeal.AdjustmentId.Value);
                    if (penalty != null && !penalty.IsDeleted)
                    {
                        // Optionally: create penalty history record here if you have such repo
                        // await _uow.PenaltyHistories.AddAsync(new PenaltyHistory { ... });

                        penalty.Points = 0;
                        penalty.Reason = (penalty.Reason ?? "") + " (Reverted by approved appeal)";
                        penalty.UpdatedAt = DateTime.UtcNow;

                        _uow.PenaltiesBonuses.Update(penalty);
                    }
                }

                if (appeal.AppealType == AppealType.Score && appeal.ScoreId.HasValue)
                {
                    // lấy đúng điểm bị appeal
                    var score = await _uow.Scores.GetByIdAsync(appeal.ScoreId.Value);
                    if (score == null)
                        throw new InvalidOperationException("Score not found.");

                    // tạo lịch sử score trước khi re-score
                    var history = new ScoreHistory
                    {
                        ScoreId = score.ScoreId,
                        SubmissionId = score.SubmissionId,
                        JudgeId = score.JudgeId,
                        CriteriaId = score.CriteriaId,
                        OldScore = (int)score.Score1,
                        OldComment = score.Comment,
                        ChangedAt = DateTime.UtcNow,
                        ChangeReason = "Appeal approved - requires re-score",
                        ChangedBy = reviewerUserId
                    };

                    await _uow.ScoreHistorys.AddAsync(history);

                    // mark require re-score + update comment
                    score.RequiresReScore = true;
                    score.Comment = (score.Comment ?? "") + " (Requires re-score due to approved appeal)";
                    _uow.Scores.Update(score);
                }
            }

            _uow.Appeals.Update(appeal);
            await _uow.SaveAsync();

            // reload for mapping
            var updated = await _uow.Appeals.GetByIdIncludingAsync(a => a.AppealId == appealId,
                a => a.Team,
                a => a.Score,
                a => a.ReviewedBy,
                a => a.Adjustment); ;
            return _mapper.Map<AppealResponseDto>(updated);
        }
    }
}
