using Common.DTOs.PrizeDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IPrizeService
    {
        Task<IEnumerable<PrizeDTO>> GetAllAsync();
        Task<IEnumerable<PrizeDTO>> GetByHackathonAsync(int hackathonId);
        Task<PrizeDTO> CreateAsync(CreatePrizeDTO dto);
        Task<PrizeDTO> UpdateAsync(UpdatePrizeDTO dto);
        Task DeleteAsync(int prizeId);
    }
}
