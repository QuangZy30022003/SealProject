using Common.DTOs.Submission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;
using System.Security.Claims;

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
            try
            {
                var result = await _submissionService.GetSubmissionsByTeamAsync(teamId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new List<object>());
            }
        }


        [HttpGet("phase/{phaseId}")]
        public async Task<IActionResult> GetByPhase(int phaseId)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                var result = await _submissionService.GetFinalSubmissionsByPhaseAsync(phaseId, userId, userRole);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _submissionService.GetSubmissionByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _submissionService.GetAllSubmissionsAsync();
            return Ok(result);
        }
        [Authorize]
        [HttpGet("team/{teamId}/phase/{phaseId}")]
        public async Task<IActionResult> GetByTeamAndPhase(int teamId, int phaseId)
        {
            try
            {
                var result = await _submissionService
                    .GetSubmissionsByTeamAndPhaseAsync(teamId, phaseId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}