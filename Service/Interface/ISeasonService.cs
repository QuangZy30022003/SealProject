using Common.DTOs.SeasonDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ISeasonService
    {
        Task<IEnumerable<SeasonResponse>> GetAllSeasonsAsync();
        Task<SeasonResponse?> GetByIdAsync(int id);
        Task<string> CreateAsync(SeasonRequest dto);
        Task<string> UpdateAsync(int id, SeasonUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
