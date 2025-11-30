using Common.DTOs.TeamInvitationDto;
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
        Task<InvitationResult> AcceptInvitationAsync(Guid invitationCode, int userId);
        Task<InvitationResult> RejectInvitationAsync(Guid invitationCode, int userId);
        Task<InvitationStatusDto> GetInvitationStatusAsync(Guid invitationCode);
        Task<List<InvitationStatusDto>> GetTeamInvitationsByTeamIdAsync(int teamId);
    }
}
