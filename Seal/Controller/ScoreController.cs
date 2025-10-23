using Common.DTOs.ScoreDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using System.Security.Claims;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScoreController : ControllerBase
    {
        private readonly IScoreService _scoreService;

        public ScoreController(IScoreService scoreService)
        {
            _scoreService = scoreService;
        }

        [HttpPost("submit")]
        [Authorize(Roles = "Judge,Admin")]
        public async Task<IActionResult> SubmitScores([FromBody] JudgeScoreDto dto)
        {
            try
            {
                // Lấy claim từ JWT
                var userIdClaim = User.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int judgeId))
                {
                    return BadRequest("Invalid or missing JudgeId in token.");
                }

                await _scoreService.SubmitJudgeScoreAsync(dto, judgeId);
                return Ok(new { message = "Scores submitted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("average/{submissionId}")]
        [Authorize]
        public async Task<IActionResult> GetAverageScores(int submissionId)
        {
            var result = await _scoreService.GetAverageScoresAsync(submissionId);
            return Ok(result);
        }

        [HttpGet("details/{submissionId}")]
        [Authorize(Roles = "Admin,Judge")]
        public async Task<IActionResult> GetScoreDetails(int submissionId)
        {
            var result = await _scoreService.GetAllScoresAsync(submissionId);
            return Ok(result);
        }
    }
}