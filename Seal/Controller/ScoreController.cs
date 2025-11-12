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

        //// ✅ GET: Lấy các submissions mà Judge có thể chấm trong Phase
        //[HttpGet("submissions/{phaseId}")]
        //public async Task<IActionResult> GetSubmissions(int phaseId)
        //{
        //    var role = User.FindFirstValue(ClaimTypes.Role);
        //    var userId = int.Parse(User.FindFirstValue("UserId"));

        //    if (role == "Admin")
        //    {
        //        // Admin có thể xem tất cả submissions trong phase
        //        var all = await _scoreService.GetSubmissionsForJudgeAsync(0, phaseId);
        //        return Ok(all);
        //    }

        //    var result = await _scoreService.GetSubmissionsForJudgeAsync(userId, phaseId);
        //    return Ok(result);
        //}

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



        // ✅ GET: Lấy danh sách điểm đã chấm
        [HttpGet("myscores/{phaseId}")]
        public async Task<IActionResult> GetMyScores(int phaseId)
        {
            var judgeId = int.Parse(User.FindFirstValue("UserId"));
            var scores = await _scoreService.GetScoresByJudgeAsync(judgeId, phaseId);
            return Ok(scores);
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

        [HttpGet("submission/{submissionId}/scores-with-average")]
        [Authorize]
        public async Task<IActionResult> GetScoresWithTeamAverage(int submissionId)
        {
            try
            {
                var result = await _scoreService.GetScoresWithTeamAverageBySubmissionAsync(submissionId);
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


    }
}