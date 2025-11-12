using Common.DTOs.GroupDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost("auto-create")]
        [Authorize(Roles = "Admin")] // Chỉ Admin mới được phép
        public async Task<IActionResult> AutoCreateGroups([FromBody] CreateGroupsRequestDto dto)
        {
            try
            {
                if (dto.TeamsPerGroup <= 0)
                    return BadRequest(new { message = "TeamsPerGroup phải lớn hơn 0" });

                // Lấy userId từ JWT
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Invalid token" });

                var groups = await _groupService.CreateGroupsByTrackAsync(dto);

                return Ok(groups);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }
    }
}