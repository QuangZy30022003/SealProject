using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ITeamInvitationService
    {
        Task<string> InviteMemberAsync(int teamId, string email, int inviterUserId);
        Task<string> AcceptInvitationAsync(Guid invitationCode, int userId);
    }
}
