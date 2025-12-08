using Common.DTOs.ScheduleEventDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IScheduleEventService
    {
        Task<List<ScheduleEventDto>> GetEventsByHackathonAsync(int hackathonId);
        Task<List<ScheduleEventDto>> GetEventsByPhaseAsync(int phaseId);
        Task<ScheduleEventDto?> GetByIdAsync(int id);
        Task<ScheduleEventDto> CreateAsync(ScheduleEventCreateDto dto);
        Task<bool> UpdateAsync(int id, ScheduleEventUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
