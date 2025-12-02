using Common.DTOs.RegisterHackathonDto;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IHackathonRegistrationService
    {
        Task<string> RegisterTeamAsync(int userId, int hackathonId, string link);
        Task<string> CancelRegistrationAsync(int userId, int hackathonId, string reason);

        Task<string> RestoreRegistrationAsync(int userId, int hackathonId);
        Task<string> ApproveTeamAsync(int chapterId, int hackathonId, int teamId);
        Task<string> RejectTeamAsync(int chapterId, int hackathonId, int teamId, string cancelReason);

        Task<List<HackathonRegistrationDto>> GetRegistrationsByHackathonAsync(int hackathonId);
        Task<object> GetPendingRegistrationsAsync(int hackathonId);
        Task<List<HackathonRegistrationDto>> GetMyRegistrationsAsync(int userId);
    }
}
