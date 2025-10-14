using Common.DTOs.PenaltyBonusDto;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IPenaltyService
    {
        Task<PenaltiesBonuseResponseDto> CreateAsync(CreatePenaltiesBonuseDto dto);
        Task<IEnumerable<PenaltiesBonuseResponseDto>> GetAllAsync();
        Task<IEnumerable<PenaltiesBonuseResponseDto>> GetByTeamAsync(int teamId);
        Task<PenaltiesBonuseResponseDto?> GetByIdAsync(int id);
        Task<PenaltiesBonuseResponseDto?> UpdateAsync(int id, UpdatePenaltiesBonuseDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
