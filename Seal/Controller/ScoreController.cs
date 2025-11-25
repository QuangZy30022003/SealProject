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

      

        // ✅ POST: Judge chấm điểm submission
        [Authorize(Roles = "Judge")]
        [HttpPost("score")]
        public async Task<IActionResult> CreateOrUpdateScores([FromBody] SubmissionScoreInputDto dto)
        {
            if (dto == null || dto.Scores == null || !dto.Scores.Any())
                return BadRequest(new { message = "No scores provided." });

            var judgeId = int.Parse(User.FindFirstValue("UserId"));

            // Map scores về ScoreCreateDto
            var scoreDtos = dto.Scores.Select(s => new ScoreCreateDto
            {
                SubmissionId = dto.SubmissionId,
                CriteriaId = s.CriteriaId,
                ScoreValue = s.ScoreValue,
                Comment = s.Comment
            }).ToList();

            try
            {
                var result = await _scoreService.CreateOrUpdateScoresAsync(judgeId, scoreDtos);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }



      
        [Authorize(Roles = "Judge")]
        [HttpPut("score")]
        public async Task<IActionResult> UpdateScores([FromBody] SubmissionScoreInputDto dto)
        {
            if (dto == null || dto.Scores == null || !dto.Scores.Any())
                return BadRequest(new { message = "No scores provided." });

            var judgeId = int.Parse(User.FindFirstValue("UserId"));
            var scoreDtos = dto.Scores.Select(s => new ScoreCreateDto
            {
                SubmissionId = dto.SubmissionId,
                CriteriaId = s.CriteriaId,
                ScoreValue = s.ScoreValue,
                Comment = s.Comment
            }).ToList();

            try
            {
                var result = await _scoreService.UpdateScoresByCriteriaAsync(judgeId, scoreDtos);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

 

        [HttpGet("group/{groupId}/team-scores")]
        [Authorize(Roles = "Judge,Admin")]
        public async Task<IActionResult> GetTeamScoresByGroup(int groupId)
        {
            try
            {
                var result = await _scoreService.GetTeamScoresByGroupAsync(groupId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("myscores/grouped/{phaseId}")]
        [Authorize(Roles = "Judge")]

        public async Task<IActionResult> GetMyScoresGrouped(int phaseId)
        {
            var judgeId = int.Parse(User.FindFirstValue("UserId"));

            try
            {
                var result = await _scoreService.GetMyScoresGroupedBySubmissionAsync(judgeId, phaseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}