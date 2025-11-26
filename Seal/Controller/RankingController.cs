using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class RankingController : ControllerBase
    {
        private readonly IRankingService _rankingService;

        public RankingController(IRankingService rankingService)
        {
            _rankingService = rankingService;
        }

        [HttpGet("hackathon/{hackathonId}")]
        [Authorize]
        public async Task<IActionResult> GetByHackathon(int hackathonId)
        {
            var data = await _rankingService.GetRankingsByHackathonAsync(hackathonId);
            return Ok(data);
        }
    }
}
