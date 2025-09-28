using AutoMapper;
using Common.DTOs.HackathonPhaseDto;
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
    public class HackathonPhaseService : IHackathonPhaseService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public HackathonPhaseService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<List<HackathonPhaseDto>> GetPhasesByHackathonAsync(int hackathonId)
        {
            var phases = await _uow.HackathonPhaseRepository.GetByHackathonIdAsync(hackathonId);
            return _mapper.Map<List<HackathonPhaseDto>>(phases);
        }

        public async Task<HackathonPhaseDto?> GetByIdAsync(int id)
        {
            var phase = await _uow.HackathonPhaseRepository.GetByIdAsync(id);
            return _mapper.Map<HackathonPhaseDto?>(phase);
        }

        public async Task<HackathonPhaseDto> CreateAsync(HackathonPhaseCreateDto dto)
        {
            if (dto.StartDate >= dto.EndDate)
                throw new ArgumentException("Phase start date must be before end date");

            // Lấy tất cả phase của hackathon
            var existingPhases = await _uow.HackathonPhaseRepository.GetByHackathonIdAsync(dto.HackathonId);

            // Check overlap
            foreach (var existing in existingPhases)
            {
                if (!(dto.EndDate <= existing.StartDate || dto.StartDate >= existing.EndDate))
                {
                    throw new ArgumentException(
                        $"Phase overlaps with existing phase (ID {existing.PhaseId}, {existing.StartDate:yyyy-MM-dd HH:mm} - {existing.EndDate:yyyy-MM-dd HH:mm})"
                    );
                }
            }

            var phase = _mapper.Map<HackathonPhase>(dto);
            await _uow.HackathonPhaseRepository.AddAsync(phase);
            await _uow.SaveAsync();
            return _mapper.Map<HackathonPhaseDto>(phase);
        }

        public async Task<bool> UpdateAsync(int id, HackathonPhaseUpdateDto dto)
        {
            var phase = await _uow.HackathonPhaseRepository.GetByIdAsync(id);
            if (phase == null) return false;

            // validate
            if (dto.StartDate >= dto.EndDate)
                throw new Exception("StartDate must be before EndDate");

            var existingPhases = await _uow.HackathonPhaseRepository.GetByHackathonIdAsync(phase.HackathonId.Value);

            bool overlap = existingPhases.Any(p =>
                p.PhaseId != id &&
                dto.StartDate < p.EndDate &&
                dto.EndDate > p.StartDate);

            if (overlap)
                throw new Exception("Phase dates must not overlap with other phases");

            // update fields
            phase.PhaseName = dto.PhaseName;
            phase.StartDate = dto.StartDate;
            phase.EndDate = dto.EndDate;

            _uow.HackathonPhaseRepository.Update(phase);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var phase = await _uow.HackathonPhaseRepository.GetByIdAsync(id);
            if (phase == null) return false;

            _uow.HackathonPhaseRepository.Remove(phase);
            await _uow.SaveAsync();
            return true;
        }
    }
}
