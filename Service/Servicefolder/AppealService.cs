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
        private readonly INotificationService _notificationService;
        private readonly IScoreService _scoreService;

        public AppealService(IUOW uow, IMapper mapper, INotificationService notificationService, IScoreService scoreService)
        {
            _uow = uow;
            _mapper = mapper;
            _notificationService = notificationService;
            _scoreService = scoreService;
        }

        public async Task<IEnumerable<AppealResponseDto>> GetAppealsByTeamAndPhaseAsync(int teamId, int phaseId)
        {
            var appeals = await _uow.Appeals.GetAllAsync(
                a =>
                    a.TeamId == teamId &&
                    (
                        (a.Submission != null && a.Submission.PhaseId == phaseId) ||
                        (a.Adjustment != null && a.Adjustment.PhaseId == phaseId)
                    ),
                includeProperties: "Team,Adjustment,Submission,Judge,ReviewedBy"
            );

            return _mapper.Map<IEnumerable<AppealResponseDto>>(appeals);
        }

        public async Task<AppealResponseDto> CreateAppealAsync(CreateAppealDto dto, int currentUserId)
        {
            // 1️ Check user is a team member
            var isMember = await _uow.TeamMembers.ExistsAsync(m => m.TeamId == dto.TeamId && m.UserId == currentUserId);
            if (!isMember)
                throw new Exception("You are not a member of this team.");

            // 1.5️ Check user is TeamLeader
            var team = await _uow.Teams.GetByIdAsync(dto.TeamId);
            if (team == null)
                throw new Exception("Team not found.");
            
            if (team.TeamLeaderId != currentUserId)
                throw new Exception("Only the team leader can create appeals.");

            // 2) Validate appeal type and required fields
            if (dto.AppealType == AppealType.Penalty)
            {
                if (!dto.AdjustmentId.HasValue)
                    throw new Exception("AdjustmentId is required for penalty appeals.");

                dto.SubmissionId = null;
                dto.JudgeId = null;
            }
            else if (dto.AppealType == AppealType.Score)
            {
                if (!dto.SubmissionId.HasValue || !dto.JudgeId.HasValue)
                    throw new Exception("SubmissionId and JudgeId are required for score appeals.");

                dto.AdjustmentId = null;
            }
            else
            {
                throw new Exception("Invalid AppealType.");
            }

            // 3) Validate referenced objects
            if (dto.AppealType == AppealType.Penalty)
            {
                var adj = await _uow.PenaltiesBonuses.GetByIdAsync(dto.AdjustmentId!.Value);
                if (adj == null)
                    throw new Exception("Penalty/Bonus not found.");

                if (adj.TeamId != dto.TeamId)
                    throw new Exception("You cannot appeal adjustment belonging to another team.");

                // ✅ Kiểm tra deadline appeal cho penalty (7 ngày sau khi penalty được tạo)
                if (adj.CreatedAt.AddDays(7) < DateTime.UtcNow)
                    throw new Exception("Appeal deadline has passed. You can only appeal within 7 days of penalty creation.");
            }
            else // Score appeal
            {
                var submission = await _uow.Submissions.GetByIdAsync(dto.SubmissionId!.Value);

                if (submission == null)
                    throw new Exception("Submission not found.");

                if (submission.TeamId != dto.TeamId)
                    throw new Exception("You cannot appeal score belonging to another team.");

                // Check that the score exists for this submission + judge
                var hasScore = await _uow.Scores.ExistsAsync(
                    s => s.SubmissionId == dto.SubmissionId && s.JudgeId == dto.JudgeId
                );

                if (!hasScore)
                    throw new Exception("Score not found for this Submission + Judge.");

                // ✅ Kiểm tra deadline appeal cho score (7 ngày sau khi phase kết thúc)
                var phase = await _uow.HackathonPhases.GetByIdAsync(submission.PhaseId);
                if (phase == null)
                    throw new Exception("Phase not found.");

                if (phase.EndDate.HasValue)
                {
                    var appealDeadline = phase.EndDate.Value.AddDays(7);
                    if (DateTime.UtcNow > appealDeadline)
                        throw new Exception($"Appeal deadline has passed. You can only appeal within 7 days after phase ends (deadline: {appealDeadline:yyyy-MM-dd HH:mm}).");
                }
            }

            // 4) Prevent duplicate pending appeals
            bool duplicateExists = await _uow.Appeals.ExistsAsync(a =>
               a.TeamId == dto.TeamId &&
               a.AppealType == dto.AppealType &&
               a.Status == AppealStatus.Pending &&
                (
                    (dto.AdjustmentId.HasValue && a.AdjustmentId == dto.AdjustmentId) ||
                    (dto.SubmissionId.HasValue && dto.JudgeId.HasValue &&
                     a.SubmissionId == dto.SubmissionId && a.JudgeId == dto.JudgeId)
                )
           );

            if (duplicateExists)
                throw new InvalidOperationException("An active pending appeal already exists for this target.");

            // 5) Create appeal
            var appeal = new Appeal
            {
                AppealType = dto.AppealType,
                AdjustmentId = dto.AdjustmentId,
                SubmissionId = dto.SubmissionId,
                JudgeId = dto.JudgeId,
                TeamId = dto.TeamId,
                Message = dto.Message,
                Reason = dto.Reason,
                Status = AppealStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Appeals.AddAsync(appeal);
            await _uow.SaveAsync();

            // 6) Reload for mapping
            var created = await _uow.Appeals.GetByIdIncludingAsync(
                a => a.AppealId == appeal.AppealId,
                a => a.Team,
                a => a.Submission,
                a => a.Judge,
                a => a.Adjustment,
                a => a.ReviewedBy
            );

            // 7) ✅ Send Notification for Admins
            var admins = await _uow.Users.GetAllAsync(u => u.RoleId == 2); // Admin role
            var adminIds = admins.Select(a => a.UserId).ToList();

            await _notificationService.CreateNotificationsAsync(
                adminIds,
                $"New appeal from {created!.Team.TeamName} requires review"
            );

            return _mapper.Map<AppealResponseDto>(created);
        }

        public async Task<IEnumerable<AppealResponseDto>> GetAppealsByTeamAsync(int teamId)
        {
            var appeals = await _uow.Appeals.GetAllAsync(
                a => a.TeamId == teamId,
                includeProperties: "Team,Adjustment,Submission,Judge,ReviewedBy"
            );

            return _mapper.Map<IEnumerable<AppealResponseDto>>(appeals);
        }

        public async Task<IEnumerable<AppealResponseDto>> GetAllAppealsAsync()
        {
            var appeals = await _uow.Appeals.GetAllAsync(
                includeProperties: "Team,Adjustment,Submission,Judge,ReviewedBy"
            );
            return _mapper.Map<IEnumerable<AppealResponseDto>>(appeals);
        }

        public async Task<IEnumerable<AppealResponseDto>> GetAppealsByPhaseAsync(int phaseId)
        {
            // Validate phase exists
            var phase = await _uow.HackathonPhases.GetByIdAsync(phaseId);
            if (phase == null)
                throw new Exception("Phase not found.");

            // Lấy tất cả submission trong phase
            var submissions = await _uow.Submissions.GetAllAsync(s => s.PhaseId == phaseId);
            var submissionIds = submissions.Select(s => s.SubmissionId).ToList();

            // Lấy tất cả penalty/bonus trong phase
            var penalties = await _uow.PenaltiesBonuses.GetAllAsync(p => p.PhaseId == phaseId);
            var penaltyIds = penalties.Select(p => p.AdjustmentId).ToList();

            // Lấy appeals liên quan đến phase này (cả score appeals và penalty appeals)
            var appeals = await _uow.Appeals.GetAllAsync(
                a => (a.SubmissionId.HasValue && submissionIds.Contains(a.SubmissionId.Value)) ||
                     (a.AdjustmentId.HasValue && penaltyIds.Contains(a.AdjustmentId.Value)),
                includeProperties: "Team,Adjustment,Submission,Judge,ReviewedBy"
            );

            return _mapper.Map<IEnumerable<AppealResponseDto>>(appeals);
        }

        public async Task<AppealResponseDto?> GetAppealByIdAsync(int appealId)
        {
            var appeal = await _uow.Appeals.GetByIdIncludingAsync(
                a => a.AppealId == appealId,
                a => a.Team,
                a => a.Submission,
                a => a.Judge,
                a => a.Adjustment,
                a => a.ReviewedBy
            );

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

            var team = await _uow.Teams.GetByIdAsync(appeal.TeamId);
            var teamMembers = await _uow.TeamMembers.GetAllAsync(tm => tm.TeamId == appeal.TeamId);
            var teamMemberIds = teamMembers.Select(tm => tm.UserId).ToList();
            // ───────────────────────────────
            // Business logic when APPROVED
            // ───────────────────────────────
            // 3️ If approved → apply business logic
            if (dto.Status == AppealStatus.Approved)
            {
                //if (appeal.AppealType == AppealType.Penalty && appeal.AdjustmentId.HasValue)
                //{
                //    // revert penalty (do not delete)
                //    var penalty = await _uow.PenaltiesBonuses.GetByIdAsync(appeal.AdjustmentId.Value);
                //    if (penalty != null && !penalty.IsDeleted)
                //    {
                //        // Optionally: create penalty history record here if you have such repo
                //        // await _uow.PenaltyHistories.AddAsync(new PenaltyHistory { ... });

                //        penalty.Points = 0;
                //        penalty.Reason = (penalty.Reason ?? "") + " (Reverted by approved appeal)";
                //        penalty.UpdatedAt = DateTime.UtcNow;

                //        _uow.PenaltiesBonuses.Update(penalty);
                //    }
                //}

                if (appeal.AppealType == AppealType.Penalty && appeal.AdjustmentId.HasValue)
                {
                    var penalty = await _uow.PenaltiesBonuses.GetByIdAsync(appeal.AdjustmentId.Value);
                    if (penalty != null && !penalty.IsDeleted)
                    {
                        penalty.Points = 0;
                        penalty.Reason = (penalty.Reason ?? "") + " (Reverted by approved appeal)";
                        penalty.UpdatedAt = DateTime.UtcNow;
                        _uow.PenaltiesBonuses.Update(penalty);

                        // 🔥 RE-CALCULATE SCORE
                        var phase = await _uow.HackathonPhases.GetByIdAsync(penalty.PhaseId);
                        if (phase == null)
                            throw new InvalidOperationException("Phase not found.");

                        var allPhases = await _uow.HackathonPhases
                            .GetAllAsync(p => p.HackathonId == phase.HackathonId);

                        var finalPhase = allPhases
                            .OrderByDescending(p => p.EndDate)
                            .First();

                    

                        bool isFinal = phase.PhaseId == finalPhase.PhaseId;

                        var submission = await _uow.Submissions.FirstOrDefaultAsync(s =>
                            s.TeamId == penalty.TeamId &&
                            s.PhaseId == penalty.PhaseId &&
                             s.IsFinal == true
                        );

                        if (submission != null)
                        {
                            if (!isFinal)
                                await _scoreService.UpdateAverageAndRankAsync(submission.SubmissionId);
                            else
                                await _scoreService.UpdateFinalRankingAsync(submission, phase.HackathonId);
                        }
                    }
                }


                if (appeal.AppealType == AppealType.Score)
                {
                    // Retrieve all score rows for this Submission + Judge
                    var scoreList = await _uow.Scores.GetAllAsync(s =>
                        s.SubmissionId == appeal.SubmissionId &&
                        s.JudgeId == appeal.JudgeId);

                    if (!scoreList.Any())
                        throw new InvalidOperationException("No scores found to update for this appeal.");

                    foreach (var score in scoreList)
                    {
                        // Save score history
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

                        // Mark require rescore
                        score.RequiresReScore = true;
                        score.Comment = (score.Comment ?? "") + " (Requires re-score due to approved appeal)";

                        _uow.Scores.Update(score);
                    }
                }
                // ✅ GỬI NOTIFICATION KHI APPROVED
                await _notificationService.CreateNotificationsAsync(
                    teamMemberIds,
                    $"Your appeal has been approved. {dto.AdminResponse}"
                );

            }
            // ✅ GỬI NOTIFICATION KHI REJECTED
            if (dto.Status == AppealStatus.Rejected)
            {
                await _notificationService.CreateNotificationsAsync(
                    teamMemberIds,
                    $"Your appeal has been rejected. {dto.AdminResponse}"
                );
            }

            _uow.Appeals.Update(appeal);
            await _uow.SaveAsync();

            // Reload entity with related data for DTO mapping
            var updated = await _uow.Appeals.GetByIdIncludingAsync(
                 a => a.AppealId == appealId,
                 a => a.Team,
                 a => a.Submission,
                 a => a.Judge,
                 a => a.ReviewedBy,
                 a => a.Adjustment
             );
            return _mapper.Map<AppealResponseDto>(updated);
        }
    }
}
