using Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamMemberController : ControllerBase
    {
        private readonly ITeamMemberService _teamMemberService;
        private readonly IUserContextService _userContext;

        public TeamMemberController(ITeamMemberService teamMemberService, IUserContextService userContext)
        {
            _teamMemberService = teamMemberService;
            _userContext = userContext;
        }

        [HttpPost("{teamId}/kick/{memberId}")]
        public async Task<IActionResult> KickMember(int teamId, int memberId)
        {
            var currentUserId = _userContext.GetCurrentUserId();
            var message = await _teamMemberService.KickMemberAsync(teamId, memberId, currentUserId);
            return Ok(new { message });
        }

        [HttpPost("{teamId}/leave")]
        public async Task<IActionResult> LeaveTeam(int teamId)
        {
            var userId = _userContext.GetCurrentUserId();
            var message = await _teamMemberService.LeaveTeamAsync(teamId, userId);
            return Ok(new { message });
        }

        //[HttpPost("{teamId}/change-role/{memberId}")]
        //public async Task<IActionResult> ChangeRole(int teamId, int memberId, [FromBody] string newRole)
        //{
        //    var currentUserId = _userContext.GetCurrentUserId();
        //    var message = await _teamMemberService.ChangeRoleAsync(teamId, memberId, newRole, currentUserId);
        //    return Ok(new { message });
        //}

        [HttpGet("{teamId}/members")]
        public async Task<IActionResult> GetMembers(int teamId)
        {
            var members = await _teamMemberService.GetTeamMembersAsync(teamId);
            return Ok(members);
        }
    }
}
