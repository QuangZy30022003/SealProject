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
        Task<PenaltiesBonuseResponseDto> CreateAsync(CreatePenaltiesBonuseDto dto, int userId);
        Task<IEnumerable<PenaltiesBonuseResponseDto>> GetAllAsync();
        Task<IEnumerable<PenaltiesBonuseResponseDto>> GetByPhaseAsync(int phaseId);
        Task<IEnumerable<PenaltiesBonuseResponseDto>> GetByTeamAsync(int teamId, int phaseId);
        Task<PenaltiesBonuseResponseDto?> GetByIdAsync(int id);
        Task<PenaltiesBonuseResponseDto?> UpdateAsync(int id, UpdatePenaltiesBonuseDto dto);
        Task<bool> SoftDeleteAsync(int id);
    }
}
