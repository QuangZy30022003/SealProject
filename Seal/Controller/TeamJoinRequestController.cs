using Common;
using Common.DTOs.TeamJoinRequestDto;
using Common.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeamJoinRequestController : ControllerBase
    {
        private readonly ITeamJoinRequestService _joinRequestService;
        private readonly IUserContextService _userContext;

        public TeamJoinRequestController(ITeamJoinRequestService joinRequestService, IUserContextService userContext)
        {
            _joinRequestService = joinRequestService;
            _userContext = userContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateJoinRequest([FromBody] CreateJoinRequestDto dto)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var result = await _joinRequestService.CreateJoinRequestAsync(dto, userId);
                return Ok(ApiResponse<JoinRequestResponseDto>.Ok(result, "Join request created successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetJoinRequestsForTeam(int teamId)
        {
            try
            {
                var result = await _joinRequestService.GetJoinRequestsForTeamAsync(teamId);
                return Ok(ApiResponse<IEnumerable<JoinRequestResponseDto>>.Ok(result, "Join requests retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyJoinRequests()
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var result = await _joinRequestService.GetJoinRequestsByUserAsync(userId);
                return Ok(ApiResponse<IEnumerable<JoinRequestResponseDto>>.Ok(result, "Your join requests retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPut("{requestId}/respond")]
        public async Task<IActionResult> RespondToJoinRequest(int requestId, [FromBody] RespondToJoinRequestDto dto)
        {
            try
            {
                var leaderId = _userContext.GetCurrentUserId();
                var result = await _joinRequestService.RespondToJoinRequestAsync(requestId, dto, leaderId);
                
                if (result == null)
                    return NotFound(ApiResponse<object>.Fail("Join request not found."));

                return Ok(ApiResponse<JoinRequestResponseDto>.Ok(result, "Response submitted successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetJoinRequestById(int requestId)
        {
            try
            {
                var result = await _joinRequestService.GetJoinRequestByIdAsync(requestId);
                
                if (result == null)
                    return NotFound(ApiResponse<object>.Fail("Join request not found."));

                return Ok(ApiResponse<JoinRequestResponseDto>.Ok(result, "Join request retrieved successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }
    }
}
