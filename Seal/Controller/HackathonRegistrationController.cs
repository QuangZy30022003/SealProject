using Common.DTOs.RegisterHackathonDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class HackathonRegistrationController : ControllerBase
    {
        private readonly IHackathonRegistrationService _service;

        public HackathonRegistrationController(IHackathonRegistrationService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        [Authorize]
        public async Task<IActionResult> Register([FromBody] RegisterHackathonRequest dto)
        {
            int userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            var result = await _service.RegisterTeamAsync(userId, dto.HackathonId, dto.Link);

            if (result.StartsWith("Team successfully"))
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }
        [HttpPost("cancel")]
        [Authorize]
        public async Task<IActionResult> Cancel([FromBody] CancelHackathonRegistrationRequest dto)
        {
            int userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            var result = await _service.CancelRegistrationAsync(userId, dto.HackathonId, dto.CancelReason);

            if (result.Contains("cancelled", StringComparison.OrdinalIgnoreCase))
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }
        [HttpPost("restoreregis")]
        [Authorize]
        public async Task<IActionResult> Restore([FromBody] RestoreHackathonRegistrationRequest dto)
        {
            int userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            var result = await _service.RestoreRegistrationAsync(userId, dto.HackathonId);

            if (result.Contains("restored", StringComparison.OrdinalIgnoreCase))
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }

        [HttpPost("approve")]
        [Authorize(Roles = "Admin,ChapterLeader")] // chỉ Admin mới được approve
        public async Task<IActionResult> Approve([FromBody] ApproveTeamRequest dto)
        {
            int chapterId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            var result = await _service.ApproveTeamAsync(chapterId, dto.HackathonId, dto.TeamId);

            if (result.Contains("approved", StringComparison.OrdinalIgnoreCase))
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }
        [HttpPost("reject")]
        [Authorize(Roles = "Admin,ChapterLeader")] // chỉ admin mới được reject
        public async Task<IActionResult> Reject([FromBody] RejectTeamRequest dto)
        {
            int chapterId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            var result = await _service.RejectTeamAsync(chapterId, dto.HackathonId, dto.TeamId, dto.CancelReason);

            if (result.Contains("rejected", StringComparison.OrdinalIgnoreCase))
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }
        [HttpGet("hackathonId")]
        [Authorize(Roles = "Admin,ChapterLeader")] // ai có quyền xem
        public async Task<IActionResult> GetByHackathon([FromQuery] int hackathonId)
        {
            var registrations = await _service.GetRegistrationsByHackathonAsync(hackathonId);
            return Ok(registrations);
        }
        [HttpGet("pending/{hackathonId}")]
        [Authorize(Roles = "Admin,ChapterLeader")]
        public async Task<IActionResult> GetPendingRegistrations(int hackathonId)
        {
            var result = await _service.GetPendingRegistrationsAsync(hackathonId);

            if (result is string errorMessage)
                return Ok(new List<object>());

            return Ok(result);
        }
        [HttpGet("my-registrations")]
        [Authorize]
        public async Task<IActionResult> GetMyRegistrations()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim);

            var registrations = await _service.GetMyRegistrationsAsync(userId);

            return Ok(registrations);
        }

    }
}