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

        public async Task<PenaltiesBonuseResponseDto> CreateAsync(CreatePenaltiesBonuseDto dto, int userId)
        {
            // Validate team
            var team = await _uow.Teams.GetByIdAsync(dto.TeamId);
            if (team == null)
                throw new Exception("Team does not exist.");

            // Validate phase
            var phase = await _uow.HackathonPhases.GetByIdAsync(dto.PhaseId);
            if (phase == null)
                throw new Exception("Phase does not exist.");

            // Team must belong to same hackathon as phase
            if (phase.HackathonId != team.HackathonId)
                throw new Exception("Team does not belong to this hackathon phase.");

            // Validate type
            var typeNorm = dto.Type?.Trim();
            if (string.IsNullOrWhiteSpace(typeNorm))
                throw new InvalidOperationException("Type is required (Penalty or Bonus).");

            if (!string.Equals(typeNorm, "Penalty", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(typeNorm, "Bonus", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Type must be 'Penalty' or 'Bonus'.");

            // Normalize points: penalties negative, bonuses positive
            if (string.Equals(typeNorm, "Penalty", StringComparison.OrdinalIgnoreCase))
                dto.Points = -Math.Abs(dto.Points);
            else
                dto.Points = Math.Abs(dto.Points);

            var entity = new PenaltiesBonuse
            {
                TeamId = dto.TeamId,
                PhaseId = dto.PhaseId,
                Type = typeNorm,
                Points = dto.Points,
                Reason = dto.Reason,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                IsDeleted = false
            };

            await _uow.PenaltiesBonuses.AddAsync(entity);
            await _uow.SaveAsync();

            return _mapper.Map<PenaltiesBonuseResponseDto>(entity);
        }

        public async Task<IEnumerable<PenaltiesBonuseResponseDto>> GetAllAsync()
        {
            var list = await _uow.PenaltiesBonuses.GetAllAsync(includeProperties: "Team,Phase");
            return _mapper.Map<IEnumerable<PenaltiesBonuseResponseDto>>(list);
        }

        public async Task<IEnumerable<PenaltiesBonuseResponseDto>> GetByPhaseAsync(int phaseId)
        {
            var list = await _uow.PenaltiesBonuses.GetAllAsync(
                x => x.PhaseId == phaseId && !x.IsDeleted,
                includeProperties: "Team,Phase");

            return _mapper.Map<IEnumerable<PenaltiesBonuseResponseDto>>(list);
        }

        public async Task<IEnumerable<PenaltiesBonuseResponseDto>> GetByTeamAsync(int teamId, int phaseId)
        {
            var list = await _uow.PenaltiesBonuses.GetAllAsync(
                x => x.TeamId == teamId && x.PhaseId == phaseId && !x.IsDeleted,
                includeProperties: "Team,Phase");

            return _mapper.Map<IEnumerable<PenaltiesBonuseResponseDto>>(list);
        }

        public async Task<PenaltiesBonuseResponseDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.PenaltiesBonuses.GetByIdIncludingAsync(
                p => p.AdjustmentId == id,
                p => p.Team,
                p => p.Phase);
            return entity == null ? null : _mapper.Map<PenaltiesBonuseResponseDto>(entity);
        }

        public async Task<PenaltiesBonuseResponseDto?> UpdateAsync(int id, UpdatePenaltiesBonuseDto dto)
        {
            var entity = await _uow.PenaltiesBonuses.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted)
                return null;

            // Normalize according to existing type (don't allow type change here)
            if (entity.Type?.Equals("Penalty", StringComparison.OrdinalIgnoreCase) == true)
                dto.Points = -Math.Abs(dto.Points);
            else
                dto.Points = Math.Abs(dto.Points);

            entity.Points = dto.Points;
            entity.Reason = dto.Reason ?? entity.Reason;
            entity.UpdatedAt = DateTime.UtcNow;

            _uow.PenaltiesBonuses.Update(entity);
            await _uow.SaveAsync();

            var updated = await _uow.PenaltiesBonuses.GetByIdIncludingAsync(p => p.AdjustmentId == id,
                p => p.Team,
                p => p.Phase);
            return _mapper.Map<PenaltiesBonuseResponseDto>(entity);
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var entity = await _uow.PenaltiesBonuses.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted)
                return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            _uow.PenaltiesBonuses.Update(entity);
            await _uow.SaveAsync();

            return true;
        }
    }
}
