using Common.DTOs.PartnerProfileDto;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IPartnerProfileService
    {
        Task<PartnerProfileDto> CreateAsync(int userId, PartnerProfileCreateDto dto);
        Task<PartnerProfileDto> UpdateAsync(int userId, PartnerProfileUpdateDto dto);
        Task<bool> DeleteAsync(int userId);
        Task<PartnerProfileDto> GetByUserIdAsync(int userId);
        Task<IEnumerable<PartnerProfileDto>> GetAllAsync();
        Task<PartnerProfileDto> GetMyProfileAsync(int userId);
        Task<PartnerProfileDto> AdminGetByUserIdAsync(int userId);

    }

}
