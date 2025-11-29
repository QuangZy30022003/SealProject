using Common.DTOs.MentorVerificationDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class MentorVerificationController : ControllerBase
    {
        private readonly IMentorVerificationService _service;

        public MentorVerificationController(IMentorVerificationService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> Create([FromForm] MentorVerificationCreateDto dto, IFormFile cvFile)
        {
            // Lấy UserId từ JWT claims
            int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            if (userId == 0) return Unauthorized("Invalid token");

            var result = await _service.CreateAsync(dto, cvFile, userId);
            return Ok(result);
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }
        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "ChapterLeader")]
        public async Task<IActionResult> Approve(int id)
        {
            int userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            try
            {
                var result = await _service.ApproveAsync(id, userId);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpPatch("{id}/reject")]
        [Authorize(Roles = "ChapterLeader")]

        public async Task<IActionResult> Reject(int id, [FromBody] string reason)
        {
            int userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            try
            {
                var result = await _service.RejectAsync(id, reason, userId);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Forbid(ex.Message);
            }
        }
        [HttpGet("hackathon/{hackathonId}/approved")]
        [Authorize]

        public async Task<IActionResult> GetApprovedMentorsByHackathon(int hackathonId)
        {
            var result = await _service.GetApprovedMentorsByHackathonAsync(hackathonId);
            return Ok(result);
        }

    }

}
