using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface ITeamInvitationRepository: IRepository<TeamInvitation>
    {
        Task<TeamInvitation?> GetByCodeAsync(Guid code);
        Task<bool> IsEmailInvitedAsync(int teamId, string email);
    }
}
