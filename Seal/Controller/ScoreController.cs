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
        private readonly IAppealScoreService _appealScoreService;

        public ScoreController(IScoreService scoreService, IAppealScoreService appealScoreService)
        {
            _scoreService = scoreService;
            _appealScoreService = appealScoreService;
        }



        // ✅ POST: Judge chấm điểm submission
        //[Authorize(Roles = "Judge")]
        //[HttpPost("score")]
        //public async Task<IActionResult> CreateOrUpdateScores([FromBody] SubmissionScoreInputDto dto)
        //{
        //    if (dto == null || dto.Scores == null || !dto.Scores.Any())
        //        return BadRequest(new { message = "No scores provided." });

        //    var judgeId = int.Parse(User.FindFirstValue("UserId"));

        //    // Map scores về ScoreCreateDto
        //    var scoreDtos = dto.Scores.Select(s => new ScoreCreateDto
        //    {
        //        SubmissionId = dto.SubmissionId,
        //        CriteriaId = s.CriteriaId,
        //        ScoreValue = s.ScoreValue,
        //        Comment = s.Comment
        //    }).ToList();

        //    try
        //    {
        //        var result = await _scoreService.CreateOrUpdateScoresAsync(judgeId, scoreDtos);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}




        //[Authorize(Roles = "Judge")]
        //[HttpPut("score")]
        //public async Task<IActionResult> UpdateScores([FromBody] SubmissionScoreInputDto dto)
        //{
        //    if (dto == null || dto.Scores == null || !dto.Scores.Any())
        //        return BadRequest(new { message = "No scores provided." });

        //    var judgeId = int.Parse(User.FindFirstValue("UserId"));
        //    var scoreDtos = dto.Scores.Select(s => new ScoreCreateDto
        //    {
        //        SubmissionId = dto.SubmissionId,
        //        CriteriaId = s.CriteriaId,
        //        ScoreValue = s.ScoreValue,
        //        Comment = s.Comment
        //    }).ToList();

        //    try
        //    {
        //        var result = await _scoreService.UpdateScoresByCriteriaAsync(judgeId, scoreDtos);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}


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
                return Ok(new List<object>());
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
        //[HttpPost]
        //[Authorize(Roles = "Judge")]
        //public async Task<IActionResult> FinalScoring(FinalScoreRequestDto request)
        //{
        //    var judgeId = int.Parse(User.FindFirstValue("UserId"));
        //    var result = await _scoreService.FinalScoringAsync(request, judgeId);
        //    return Ok(result);
        //}

        //[HttpPut("{submissionId}/final-score")]
        //[Authorize(Roles = "Judge")]
        //public async Task<ActionResult<FinalScoreResponseDto>> UpdateFinalScore(int submissionId, [FromBody] FinalScoreRequestDto request)
        //{
        //    try
        //    {
        //        // Lấy userId từ token
        //        int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        //        if (userId == 0) return Unauthorized();

        //        var result = await _scoreService.UpdateFinalScoreAsync(userId, request);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        [HttpPost("re-score/{appealId}")]
        [Authorize(Roles = "Judge, Admin")]
        public async Task<IActionResult> ReScoreAppeal(int appealId, [FromBody] FinalScoreRequestDto request)
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var result = await _appealScoreService.ReScoreAppealAsync(appealId, request, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("submit")]
        [Authorize(Roles = "Judge, Admin")]
        public async Task<IActionResult> SubmitScores([FromBody] ScoreSubmissionRequestDto request)
        {
            try
            {
                // Lấy judgeId từ token
                var judgeId = int.Parse(User.FindFirstValue("UserId"));

                var result = await _scoreService.ScoreSubmissionAsync(judgeId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        [HttpPut("scores/{scoreId}")]
        [Authorize(Roles = "Judge,Admin")]
        public async Task<IActionResult> UpdateScoreById(
     int scoreId,
     [FromBody] ScoreUpdateByIdDto request)
        {
            try
            {
                var judgeIdClaim = User.FindFirst("UserId");
                if (judgeIdClaim == null)
                    return Unauthorized();

                int judgeId = int.Parse(judgeIdClaim.Value);

                var result = await _scoreService
                    .UpdateScoreByIdAsync(judgeId, scoreId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("team-overview")]
        public async Task<IActionResult> GetTeamOverview(
    [FromQuery] int teamId,
    [FromQuery] int phaseId)
        {
            var result = await _scoreService.GetTeamOverviewAsync(teamId, phaseId);
            return Ok(result);
        }


    }
}