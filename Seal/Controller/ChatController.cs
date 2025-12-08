using Common;
using Common.DTOs.ChatDto;
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
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IUserContextService _userContext;

        public ChatController(IChatService chatService, IUserContextService userContext)
        {
            _chatService = chatService;
            _userContext = userContext;
        }
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            try
            {
                var senderId = _userContext.GetCurrentUserId();
                var result = await _chatService.SendMessageAsync(dto, senderId);
                return Ok(ApiResponse<ChatMessageDto>.Ok(result, "Message sent successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpGet("group/{chatGroupId}/messages")]
        public async Task<IActionResult> GetMessages(int chatGroupId, [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            try
            {
                // If pagination params provided, use paginated version
                if (page.HasValue || pageSize.HasValue)
                {
                    var pagedResult = await _chatService.GetMessagesPaginatedAsync(
                        chatGroupId, 
                        page ?? 1, 
                        pageSize ?? 50);
                    return Ok(ApiResponse<PagedResult<ChatMessageDto>>.Ok(pagedResult, "Messages retrieved successfully"));
                }

                // Otherwise return all messages (backward compatibility)
                var result = await _chatService.GetMessagesAsync(chatGroupId);
                return Ok(ApiResponse<IEnumerable<ChatMessageDto>>.Ok(result, "Messages retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpGet("mentor/{mentorId}/groups")]
        public async Task<IActionResult> GetChatGroupsByMentor(int mentorId)
        {
            try
            {
                var result = await _chatService.GetChatGroupsByMentorAsync(mentorId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("team/{teamId}/groups")]
        public async Task<IActionResult> GetChatGroupsByTeam(int teamId)
        {
            try
            {
                var result = await _chatService.GetChatGroupsByTeamAsync(teamId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("hackathon/{hackathonId}/groups")]
        public async Task<IActionResult> GetChatGroupsByHackathon(int hackathonId)
        {
            try
            {
                var result = await _chatService.GetChatGroupsByHackathonAsync(hackathonId);
                return Ok(ApiResponse<IEnumerable<ChatGroupDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("team/{teamId}/hackathon/{hackathonId}/groups")]
        public async Task<IActionResult> GetChatGroupsByTeamAndHackathon(int teamId, int hackathonId)
        {
            try
            {
                var result = await _chatService.GetChatGroupsByTeamAndHackathonAsync(teamId, hackathonId);
                return Ok(ApiResponse<IEnumerable<ChatGroupDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("mentor/{mentorId}/hackathon/{hackathonId}/groups")]
        public async Task<IActionResult> GetChatGroupsByMentorAndHackathon(int mentorId, int hackathonId)
        {
            try
            {
                var result = await _chatService.GetChatGroupsByMentorAndHackathonAsync(mentorId, hackathonId);
                return Ok(ApiResponse<IEnumerable<ChatGroupDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("group/{chatGroupId}/read")]
        public async Task<IActionResult> MarkAsRead(int chatGroupId)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                await _chatService.MarkAsReadAsync(chatGroupId, userId);
                return Ok(new { message = "Messages marked as read" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
