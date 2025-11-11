using Common.DTOs.Submission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;

namespace Seal.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }
        [Authorize]
        [HttpPost("draft")]
        public async Task<IActionResult> CreateDraft([FromBody] SubmissionCreateDto dto)
        {
            try
            {
                // Lấy UserId trực tiếp từ token JWT
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (userIdClaim == null) return Unauthorized("Invalid token");

                int userIdFromToken = int.Parse(userIdClaim);

                var result = await _submissionService.CreateDraftAsync(dto, userIdFromToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [Authorize]
        [HttpPut("draft/{submissionId}")]
        public async Task<IActionResult> UpdateDraft(int submissionId, [FromBody] SubmissionUpdateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (userIdClaim == null) return Unauthorized("Invalid token");

                int userIdFromToken = int.Parse(userIdClaim);

                var result = await _submissionService.UpdateDraftAsync(submissionId, dto, userIdFromToken);
                if (result == null) return NotFound("Draft not found or is final");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("set-final")]
        public async Task<IActionResult> SetFinal([FromBody] SubmissionFinalDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (userIdClaim == null) return Unauthorized("Invalid token");

                int userIdFromToken = int.Parse(userIdClaim);

                var result = await _submissionService.SetFinalAsync(dto, userIdFromToken);
                if (result == null) return NotFound("Submission not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetByTeam(int teamId)
        {
            var result = await _submissionService.GetSubmissionsByTeamAsync(teamId);
            return Ok(result);
        }
    }
}