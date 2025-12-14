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

        [Authorize]
        [HttpPost("{teamId}/invite")]
        public async Task<IActionResult> InviteMember(int teamId, [FromBody] InviteMemberRequest request)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var link = await _invitationService.InviteMemberAsync(teamId, request.Email, userId);

                return Ok(ApiResponse<object>.Ok(new { link }, "Invitation sent successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        // POST: api/TeamInvitation/accept-link?code=guid
        [Authorize]
        [HttpPost("accept-link")]
        public async Task<IActionResult> AcceptInvitation([FromQuery] Guid code)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var result = await _invitationService.AcceptInvitationAsync(code, userId);

                // ✅ Nếu thất bại (nghiệp vụ) thì trả về 409 Conflict (hoặc 400 tùy chính sách)
                if (result.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                    return Conflict(ApiResponse<InvitationResult>.Ok(result, result.Message));


                // ✅ Thành công hoàn toàn
                return Ok(ApiResponse<InvitationResult>.Ok(result, result.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                // Nếu người khác dùng link invite không phải của họ
                return Unauthorized(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                // Bắt các lỗi hệ thống (invalid code, expired,...)
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }


        // POST: api/TeamInvitation/reject?code=guid
        [Authorize]
        [HttpPost("reject")]
        public async Task<IActionResult> RejectInvitation([FromQuery] Guid code)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var result = await _invitationService.RejectInvitationAsync(code, userId);

                // ✅ Nếu thất bại (nghiệp vụ) thì trả về 409 Conflict (hoặc 400 tùy chính sách)
                if (result.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                    return Conflict(ApiResponse<InvitationResult>.Ok(result, result.Message));


                // ✅ Thành công hoàn toàn
                return Ok(ApiResponse<InvitationResult>.Ok(result, result.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                // Nếu người khác dùng link invite không phải của họ
                return Unauthorized(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                // Bắt các lỗi hệ thống (invalid code, expired,...)
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        // GET: api/TeamInvitation/status?code=guid
        [Authorize]
        [HttpGet("status")]
        public async Task<IActionResult> GetInvitationStatus([FromQuery] Guid code)
        {
            try
            {
                var result = await _invitationService.GetInvitationStatusAsync(code);
                return Ok(ApiResponse<InvitationStatusDto>.Ok(result, "Invitation status retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        // GET: api/TeamInvitation/team/{teamId}
        [Authorize]
        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetTeamInvitations(int teamId)
        {
            try
            {
                var result = await _invitationService.GetTeamInvitationsByTeamIdAsync(teamId);
                return Ok(ApiResponse<List<InvitationStatusDto>>.Ok(result, "Team invitations retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

    }
}
