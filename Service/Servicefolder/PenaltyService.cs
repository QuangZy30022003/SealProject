using AutoMapper;
using Common.DTOs.PenaltyBonusDto;
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
    public class PenaltyService: IPenaltyService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public PenaltyService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<PenaltiesBonuseResponseDto> CreateAsync(CreatePenaltiesBonuseDto dto)
        {
            // Validate loại adjustment
            if (string.IsNullOrWhiteSpace(dto.Type) ||
                !(dto.Type.Equals("Penalty", StringComparison.OrdinalIgnoreCase) ||
                  dto.Type.Equals("Bonus", StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception("Type must be either 'Penalty' or 'Bonus'.");
            }

            // Nếu là penalty, điểm phải âm hoặc > 0 nhưng được trừ
            if (dto.Type.Equals("Penalty", StringComparison.OrdinalIgnoreCase) && dto.Points > 0)
            {
                dto.Points = -Math.Abs(dto.Points);
            }

            // Nếu là bonus thì điểm phải dương
            if (dto.Type.Equals("Bonus", StringComparison.OrdinalIgnoreCase) && dto.Points < 0)
            {
                dto.Points = Math.Abs(dto.Points);
            }

            var adjustment = new PenaltiesBonuse
            {
                TeamId = dto.TeamId,
              //  HackathonId = dto.HackathonId,
                Type = dto.Type,
                Points = dto.Points,
                Reason = dto.Reason,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.PenaltiesBonuses.AddAsync(adjustment);
            await _uow.SaveAsync();

            return _mapper.Map<PenaltiesBonuseResponseDto>(adjustment);
        }

        public async Task<IEnumerable<PenaltiesBonuseResponseDto>> GetAllAsync()
        {
            var list = await _uow.PenaltiesBonuses.GetAllAsync(includeProperties: "Team,Hackathon");
            return _mapper.Map<IEnumerable<PenaltiesBonuseResponseDto>>(list);
        }

        public async Task<IEnumerable<PenaltiesBonuseResponseDto>> GetByTeamAsync(int teamId)
        {
            var list = await _uow.PenaltiesBonuses.GetAllAsync(
                x => x.TeamId == teamId,
                includeProperties: "Hackathon"
            );
            return _mapper.Map<IEnumerable<PenaltiesBonuseResponseDto>>(list);
        }

        public async Task<PenaltiesBonuseResponseDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.PenaltiesBonuses.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<PenaltiesBonuseResponseDto>(entity);
        }

        public async Task<PenaltiesBonuseResponseDto?> UpdateAsync(int id, UpdatePenaltiesBonuseDto dto)
        {
            var entity = await _uow.PenaltiesBonuses.GetByIdAsync(id);
            if (entity == null) return null;

            // Giữ nguyên loại (Penalty/Bonus)
            if (entity.Type?.Equals("Penalty", StringComparison.OrdinalIgnoreCase) == true && dto.Points > 0)
                dto.Points = -Math.Abs(dto.Points);
            if (entity.Type?.Equals("Bonus", StringComparison.OrdinalIgnoreCase) == true && dto.Points < 0)
                dto.Points = Math.Abs(dto.Points);

            entity.Points = dto.Points;
            entity.Reason = dto.Reason;

            _uow.PenaltiesBonuses.Update(entity);
            await _uow.SaveAsync();

            return _mapper.Map<PenaltiesBonuseResponseDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _uow.PenaltiesBonuses.GetByIdAsync(id);
            if (entity == null) return false;

            _uow.PenaltiesBonuses.Remove(entity);
            await _uow.SaveAsync();

            return true;
        }
    }
}
