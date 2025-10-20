using Common.DTOs.CriterionDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ICriterionService
    {
        Task<IEnumerable<CriterionReadDTO>> GetByPhaseChallengeAsync(int phaseChallengeId);
        Task<CriterionReadDTO?> GetByIdAsync(int id);
        Task<CriterionReadDTO> CreateAsync(CriterionCreateDTO dto);
        Task<bool> UpdateAsync(CriterionUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<CriterionReadDTO>> GetAllAsync();
    }
}
