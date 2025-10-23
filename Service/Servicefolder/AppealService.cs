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

        public async Task<AppealResponseDto> CreateAppealAsync(CreateAppealDto dto)
        {
            // Validate appeal type và required fields
            if (dto.AppealType == AppealType.Penalty && !dto.AdjustmentId.HasValue)
                throw new Exception("AdjustmentId is required for penalty appeals.");

            if (dto.AppealType == AppealType.Score && !dto.ScoreId.HasValue)
                throw new Exception("ScoreId is required for score appeals.");

            // 1️ Check adjustment/score tồn tại và đúng team
            if (dto.AppealType == AppealType.Penalty)
            {
                var adjustment = await _uow.PenaltiesBonuses.GetByIdAsync(dto.AdjustmentId.Value);
                if (adjustment == null)
                    throw new Exception("Adjustment not found.");

                if (adjustment.TeamId != dto.TeamId)
                    throw new Exception("You cannot appeal for another team's adjustment.");
            }
            else if (dto.AppealType == AppealType.Score)
            {
                var score = await _uow.Scores.GetByIdAsync(dto.ScoreId.Value);
                if (score == null)
                    throw new Exception("Score not found.");

                // Check team thông qua submission
                if (score.Submission?.TeamId != dto.TeamId)
                    throw new Exception("You cannot appeal for another team's score.");
            }

            // 2️ Check trùng khiếu nại
            var existing = (await _uow.Appeals.GetAllAsync(a =>
                a.AppealType == dto.AppealType &&
                ((dto.AppealType == AppealType.Penalty && a.AdjustmentId == dto.AdjustmentId) ||
                 (dto.AppealType == AppealType.Score && a.ScoreId == dto.ScoreId)) &&
                a.TeamId == dto.TeamId))
                .FirstOrDefault();

            if (existing != null)
                throw new Exception($"This {dto.AppealType.ToLower()} already has an appeal from your team.");

            // 3️ Tạo appeal mới
            var appeal = new Appeal
            {
                AppealType = dto.AppealType,
                AdjustmentId = dto.AdjustmentId,
                ScoreId = dto.ScoreId,
                TeamId = dto.TeamId,
                Message = dto.Message,
                Status = AppealStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Appeals.AddAsync(appeal);
            await _uow.SaveAsync();

            return _mapper.Map<AppealResponseDto>(appeal);
        }

        public async Task<IEnumerable<AppealResponseDto>> GetAppealsByTeamAsync(int teamId)
        {
            var appeals = await _uow.Appeals.GetAllAsync(
                a => a.TeamId == teamId,
                includeProperties: "Penalty,Score,ReviewedBy"
            );

            return _mapper.Map<IEnumerable<AppealResponseDto>>(appeals);
        }

        public async Task<IEnumerable<AppealResponseDto>> GetAllAppealsAsync()
        {
            var appeals = await _uow.Appeals.GetAllAsync(
                includeProperties: "Penalty,Score,ReviewedBy,Team"
            );

            return _mapper.Map<IEnumerable<AppealResponseDto>>(appeals);
        }

        // 🟠 Lấy chi tiết 1 appeal (optional)
        public async Task<AppealResponseDto?> GetAppealByIdAsync(int appealId)
        {
            var appeal = await _uow.Appeals.GetByIdAsync(appealId);
            return appeal == null ? null : _mapper.Map<AppealResponseDto>(appeal);
        }

        public async Task<AppealResponseDto?> ReviewAppealAsync(int appealId, ReviewAppealDto dto)
        {
            var appeal = await _uow.Appeals.GetByIdAsync(appealId);
            if (appeal == null)
                return null;

            // Không được duyệt lại appeal đã có kết quả
            if (appeal.Status != AppealStatus.Pending)
                throw new Exception("This appeal has already been reviewed.");

            appeal.Status = dto.Status;
            appeal.AdminResponse = dto.AdminResponse;
            appeal.ReviewedById = dto.ReviewedById;
            appeal.ReviewedAt = DateTime.UtcNow;

            // ✅ Nếu Appeal được chấp nhận
            if (dto.Status == AppealStatus.Approved)
            {
                if (appeal.AppealType == AppealType.Penalty && appeal.AdjustmentId.HasValue)
                {
                    var adjustment = await _uow.PenaltiesBonuses.GetByIdAsync(appeal.AdjustmentId.Value);
                    if (adjustment != null)
                    {
                        // Chỉ revert nếu là Penalty
                        if (adjustment.Type?.Equals("Penalty", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            adjustment.Points = 0;
                            adjustment.Reason += " (Reverted by approved appeal)";
                            _uow.PenaltiesBonuses.Update(adjustment);
                        }
                    }
                }
                else if (appeal.AppealType == AppealType.Score && appeal.ScoreId.HasValue)
                {
                    var score = await _uow.Scores.GetByIdAsync(appeal.ScoreId.Value);
                    if (score != null)
                    {
                        // Có thể thêm logic xử lý score ở đây nếu cần
                        // Ví dụ: ghi chú lại score đã được khiếu nại
                        score.Comment += " (Score appealed and approved)";
                        _uow.Scores.Update(score);
                    }
                }
            }

            _uow.Appeals.Update(appeal);
            await _uow.SaveAsync();

            return _mapper.Map<AppealResponseDto>(appeal);
        }
    }
}
