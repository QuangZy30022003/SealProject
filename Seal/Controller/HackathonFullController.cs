using Common.DTOs.HackathonDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;
using System.Security.Claims;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HackathonFullController : ControllerBase
    {
        private readonly IHackathonFullService _service;

        public HackathonFullController(IHackathonFullService service)
        {
            _service = service;
        }
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFullHackathon([FromBody] HackathonFullCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { success = false, message = "User ID not found in token" });

                int userId = int.Parse(userIdClaim);
                var hackathonId = await _service.CreateFullHackathonAsync(dto, userId);

                return Ok(new { success = true, hackathonId, message = "Hackathon created successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}