using Common;
using Common.DTOs.TeamInvitationDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamInvitationController : ControllerBase
    {
        private readonly ITeamInvitationService _invitationService;
        private readonly IUserContextService _userContext;

        public TeamInvitationController(ITeamInvitationService invitationService, IUserContextService userContext)
        {
            _invitationService = invitationService;
            _userContext = userContext;
        }
        [Authorize]
        [HttpPost("{teamId}/invite")]
        public async Task<IActionResult> Invite(int teamId, [FromBody] InviteMemberRequest request)
        {
            var userId = _userContext.GetCurrentUserId();
            var link = await _invitationService.InviteMemberAsync(teamId, request.Email, userId);
            return Ok(new { message = "Invitation sent.", link });
        }
        [Authorize]
        [HttpGet("accept-link")]
        public async Task<IActionResult> AcceptFromLink([FromQuery] Guid code)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _invitationService.AcceptInvitationAsync(code, userId);
            return Ok(new { message = result });
        }

    }
}
