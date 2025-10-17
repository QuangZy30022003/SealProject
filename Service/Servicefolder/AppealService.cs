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
            // 1️ Check adjustment tồn tại
            var adjustment = await _uow.PenaltiesBonuses.GetByIdAsync(dto.AdjustmentId);
            if (adjustment == null)
                throw new Exception("Adjustment not found.");

            // 2️ Check đúng team
            if (adjustment.TeamId != dto.TeamId)
                throw new Exception("You cannot appeal for another team's adjustment.");

            // 3️ Check trùng khiếu nại
            var existing = (await _uow.Appeals.GetAllAsync(a =>
                a.AdjustmentId == dto.AdjustmentId && a.TeamId == dto.TeamId))
                .FirstOrDefault();

            if (existing != null)
                throw new Exception("This adjustment already has an appeal from your team.");

            // 4️ Tạo appeal mới
            var appeal = new Appeal
            {
                AdjustmentId = dto.AdjustmentId,
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
                includeProperties: "Penalty,ReviewedBy"
            );

            return _mapper.Map<IEnumerable<AppealResponseDto>>(appeals);
        }

        public async Task<IEnumerable<AppealResponseDto>> GetAllAppealsAsync()
        {
            var appeals = await _uow.Appeals.GetAllAsync(
                includeProperties: "Penalty,ReviewedBy,Team"
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
                var adjustment = await _uow.PenaltiesBonuses.GetByIdAsync(appeal.AdjustmentId);
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

            _uow.Appeals.Update(appeal);
            await _uow.SaveAsync();

            return _mapper.Map<AppealResponseDto>(appeal);
        }
    }
}
