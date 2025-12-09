using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScoreHistoryController : ControllerBase
    {
        private readonly IScoreHistoryService _service;

        public ScoreHistoryController(IScoreHistoryService service)
        {
            _service = service;
        }

        [HttpGet("hackathon/{hackathonId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByHackathon(int hackathonId)
        {
            var result = await _service.GetHistoryByHackathonAsync(hackathonId);
            return Ok(result);
        }

        [HttpGet("submission/{submissionId}")]
        [Authorize(Roles = "Admin,Judge")]
        public async Task<IActionResult> GetBySubmission(int submissionId)
        {
            var result = await _service.GetHistoryBySubmissionAsync(submissionId);
            return Ok(result);
        }
        [HttpGet("judge/{judgeId}")]
        [Authorize(Roles = "Admin,Judge")]
        public async Task<IActionResult> GetByJudge(int judgeId)
        {
            var result = await _service.GetHistoryByJudgeAsync(judgeId);
            return Ok(result);
        }

    }
}