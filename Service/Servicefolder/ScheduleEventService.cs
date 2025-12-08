using AutoMapper;
using Common.DTOs.ScheduleEventDto;
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
    public class ScheduleEventService : IScheduleEventService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public ScheduleEventService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<List<ScheduleEventDto>> GetEventsByHackathonAsync(int hackathonId)
        {
            var events = await _uow.ScheduleEvents.GetAllIncludingAsync(
                e => e.HackathonId == hackathonId,
                e => e.Hackathon,
                e => e.Phase
            );

            return _mapper.Map<List<ScheduleEventDto>>(events.OrderBy(e => e.StartTime));
        }

        public async Task<List<ScheduleEventDto>> GetEventsByPhaseAsync(int phaseId)
        {
            var events = await _uow.ScheduleEvents.GetAllIncludingAsync(
                e => e.PhaseId == phaseId,
                e => e.Hackathon,
                e => e.Phase
            );

            return _mapper.Map<List<ScheduleEventDto>>(events.OrderBy(e => e.StartTime));
        }

        public async Task<ScheduleEventDto?> GetByIdAsync(int id)
        {
            var scheduleEvent = await _uow.ScheduleEvents.GetByIdIncludingAsync(
                e => e.EventId == id,
                e => e.Hackathon,
                e => e.Phase
            );

            return _mapper.Map<ScheduleEventDto?>(scheduleEvent);
        }

        public async Task<ScheduleEventDto> CreateAsync(ScheduleEventCreateDto dto)
        {
            // Validate hackathon exists
            var hackathon = await _uow.Hackathons.GetByIdAsync(dto.HackathonId);
            if (hackathon == null)
                throw new ArgumentException($"Hackathon with ID {dto.HackathonId} does not exist.");

            // Validate phase if provided
            if (dto.PhaseId.HasValue)
            {
                var phase = await _uow.HackathonPhases.GetByIdAsync(dto.PhaseId.Value);
                if (phase == null)
                    throw new ArgumentException($"Phase with ID {dto.PhaseId} does not exist.");

                if (phase.HackathonId != dto.HackathonId)
                    throw new ArgumentException("Phase does not belong to the specified Hackathon.");
            }

            // Validate dates
            if (dto.StartTime.HasValue && dto.EndTime.HasValue && dto.StartTime >= dto.EndTime)
                throw new ArgumentException("Start time must be before end time.");

            var scheduleEvent = _mapper.Map<ScheduleEvent>(dto);
            await _uow.ScheduleEvents.AddAsync(scheduleEvent);
            await _uow.SaveAsync();

            return await GetByIdAsync(scheduleEvent.EventId) ?? _mapper.Map<ScheduleEventDto>(scheduleEvent);
        }

        public async Task<bool> UpdateAsync(int id, ScheduleEventUpdateDto dto)
        {
            var scheduleEvent = await _uow.ScheduleEvents.GetByIdAsync(id);
            if (scheduleEvent == null) return false;

            // Validate phase if provided
            if (dto.PhaseId.HasValue)
            {
                var phase = await _uow.HackathonPhases.GetByIdAsync(dto.PhaseId.Value);
                if (phase == null)
                    throw new ArgumentException($"Phase with ID {dto.PhaseId} does not exist.");

                if (phase.HackathonId != scheduleEvent.HackathonId)
                    throw new ArgumentException("Phase does not belong to the event's Hackathon.");
            }

            // Validate dates
            if (dto.StartTime.HasValue && dto.EndTime.HasValue && dto.StartTime >= dto.EndTime)
                throw new ArgumentException("Start time must be before end time.");

            // Update fields
            scheduleEvent.PhaseId = dto.PhaseId;
            scheduleEvent.Name = dto.Name;
            scheduleEvent.Type = dto.Type;
            scheduleEvent.Location = dto.Location;
            scheduleEvent.StartTime = dto.StartTime;
            scheduleEvent.EndTime = dto.EndTime;
            scheduleEvent.Description = dto.Description;

            _uow.ScheduleEvents.Update(scheduleEvent);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var scheduleEvent = await _uow.ScheduleEvents.GetByIdAsync(id);
            if (scheduleEvent == null) return false;

            _uow.ScheduleEvents.Remove(scheduleEvent);
            await _uow.SaveAsync();
            return true;
        }
    }
}
