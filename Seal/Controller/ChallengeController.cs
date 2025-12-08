using Common.DTOs.ChallengeDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChallengeController : ControllerBase
    {
        private readonly IChallengeService _service;

        public ChallengeController(IChallengeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var challenges = await _service.GetAllAsync();
            return Ok(challenges);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var challenge = await _service.GetByIdAsync(id);
            if (challenge == null) return NotFound();
            return Ok(challenge);
        }

        [Authorize(Roles = "Partner, Admin")]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] ChallengeCreateUnifiedDto dto)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var result = await _service.CreateAsync(dto, userId);

            return Ok(result);
        }

        [Authorize(Roles = "Partner, Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null) return Unauthorized("Không tìm thấy UserId trong token");
            int userId = int.Parse(userIdClaim);
            var result = await _service.PartnerDeleteAsync(id, userId);
            if (!result) return NotFound();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChallengeStatusDto statusDto)
        {
            var success = await _service.ChangeStatusAsync(id, statusDto);
            if (!success) return NotFound();
            return NoContent();
        }
        [Authorize(Roles = "Partner, Admin")]
        [HttpPut("{id}/partner")]
        public async Task<IActionResult> PartnerUpdate(int id, [FromForm] ChallengePartnerUpdateDto dto)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized("Không tìm thấy UserId trong token");

            int userId = int.Parse(userIdClaim);

            var errorMessage = await _service.PartnerUpdateAsync(id, userId, dto);
            if (errorMessage != null)
                return BadRequest(new { message = errorMessage });

            return NoContent();
        }
        [Authorize(Roles = "Admin")]

        [HttpGet("completed/{hackathonId}")]
        public async Task<IActionResult> GetCompletedChallengesByHackathon(int hackathonId)
        {
            var result = await _service.GetCompletedChallengesByHackathonAsync(hackathonId);

            return Ok(result ?? new List<ChallengeDto>());
        }

        [HttpGet("track/{trackId}")]
        public async Task<IActionResult> GetChallengesByTrack(int trackId)
        {
            try
            {
                var result = await _service.GetChallengesByTrackIdAsync(trackId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // GET: api/challenge/my/hackathon/5
        [Authorize(Roles = "Partner, Admin")]
        [HttpGet("my/challenges/{hackathonId}")]
        public async Task<IActionResult> GetMyChallengesByHackathon(int hackathonId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized("Không tìm thấy UserId trong token");

            int userId = int.Parse(userIdClaim);

            var result = await _service.GetMyChallengesByHackathonAsync(userId, hackathonId);

            return Ok(result);
        }

    }
}
