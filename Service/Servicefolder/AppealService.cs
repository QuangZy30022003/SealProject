using AutoMapper;
using Common.DTOs.AppealDto;
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
            var appeal = new Appeal
            {
                AdjustmentId = dto.AdjustmentId,
                TeamId = dto.TeamId,
                Message = dto.Message,
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

        public async Task<AppealResponseDto?> ReviewAppealAsync(int appealId, ReviewAppealDto dto)
        {
            var appeal = await _uow.Appeals.GetByIdAsync(appealId);
            if (appeal == null) return null;

            appeal.Status = dto.Status;
            appeal.AdminResponse = dto.AdminResponse;
            appeal.ReviewedById = dto.ReviewedById;
            appeal.ReviewedAt = DateTime.UtcNow;

            _uow.Appeals.Update(appeal);
            await _uow.SaveAsync();

            return _mapper.Map<AppealResponseDto>(appeal);
        }
    }
}
