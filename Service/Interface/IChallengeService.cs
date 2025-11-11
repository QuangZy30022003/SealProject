using Common.DTOs.ChallengeDto;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IChallengeService
    {
        Task<IEnumerable<ChallengeDto>> GetAllAsync();
        Task<ChallengeDto?> GetByIdAsync(int id);
        Task<ChallengeDto> CreateAsync(ChallengeCreateUnifiedDto dto, int userId);

        Task<bool> PartnerDeleteAsync(int id, int userId);
        Task<bool> ChangeStatusAsync(int id, ChallengeStatusDto statusDto);

        Task<string?> PartnerUpdateAsync(int id, int userId, ChallengePartnerUpdateDto dto);
        Task<List<ChallengeDto>> GetCompletedChallengesByHackathonAsync(int hackathonId);

    }
}
