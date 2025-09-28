using Common.DTOs.HackathonPhaseDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IHackathonPhaseService
    {
        Task<List<HackathonPhaseDto>> GetPhasesByHackathonAsync(int hackathonId);
        Task<HackathonPhaseDto?> GetByIdAsync(int id);
        Task<HackathonPhaseDto> CreateAsync(HackathonPhaseCreateDto dto);
        Task<bool> UpdateAsync(int id, HackathonPhaseUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
