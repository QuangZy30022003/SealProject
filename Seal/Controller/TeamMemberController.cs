using Common;
using Common.Wrappers;
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

        [HttpDelete("{teamId}/kick/{memberId}")]
        public async Task<IActionResult> KickMember(int teamId, int memberId)
        {
            try
            {
                var currentUserId = _userContext.GetCurrentUserId();
                var message = await _teamMemberService.KickMemberAsync(teamId, memberId, currentUserId);
                return Ok(ApiResponse<object>.Ok(new { message }, message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPost("{teamId}/leave")]
        public async Task<IActionResult> LeaveTeam(int teamId)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var message = await _teamMemberService.LeaveTeamAsync(teamId, userId);
                return Ok(ApiResponse<object>.Ok(new { message }, message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPost("{teamId}/transfer-leader/{newLeaderId}")]
        public async Task<IActionResult> ChangeLeader(int teamId, int newLeaderId)
        {
            try
            {
                var currentUserId = _userContext.GetCurrentUserId();
                var message = await _teamMemberService.ChangeLeaderAsync(teamId, newLeaderId, currentUserId);
                return Ok(ApiResponse<object>.Ok(new { message }, message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpGet("{teamId}/members")]
        public async Task<IActionResult> GetMembers(int teamId)
        {
            try
            {
                var members = await _teamMemberService.GetTeamMembersAsync(teamId);
                return Ok(ApiResponse<object>.Ok(members, "Team members retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }
    }
}
