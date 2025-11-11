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
        Task<List<CriterionResponseDto>> CreateAsync(CriterionCreateDto dto);
        Task<List<CriterionResponseDto>> GetAllAsync(int? phaseId = null);
        Task<CriterionResponseDto?> GetByIdAsync(int id);
        Task<CriterionResponseDto?> UpdateAsync(int id, CriterionUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
