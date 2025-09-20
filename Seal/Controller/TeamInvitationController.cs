using Common;
using Common.DTOs.TeamInvitationDto;
using Common.Wrappers;
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

        // POST: api/TeamInvitation/accept?code=guid
        [Authorize]
        [HttpPost("{teamId}/invite")]
        public async Task<IActionResult> Invite(int teamId, [FromBody] InviteMemberRequest request)
        {
            var userId = _userContext.GetCurrentUserId();
            var link = await _invitationService.InviteMemberAsync(teamId, request.Email, userId);
            return Ok(new { message = "Invitation sent.", link });
        }

        // POST: api/TeamInvitation/accept?code=guid
        [Authorize]
        [HttpPost("accept")]
        public async Task<IActionResult> Accept([FromQuery] Guid code)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _invitationService.AcceptInvitationAsync(code, userId);

            return Ok(ApiResponse<AcceptInvitationResult>.Ok(result, result.Message));
        }


        // POST: api/TeamInvitation/reject?code=guid
        [Authorize]
        [HttpPost("reject")]
        public async Task<IActionResult> Reject([FromQuery] Guid code)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _invitationService.RejectInvitationAsync(code, userId);
            return Ok(new { message = result });
        }
        // GET: api/TeamInvitation/status?code=guid
        [Authorize]
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus([FromQuery] Guid code)
        {
            var result = await _invitationService.GetInvitationStatusAsync(code);
            return Ok(result);
        }

    }
}
