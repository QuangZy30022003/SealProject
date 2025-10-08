using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhaseChallengeController : ControllerBase
    {
        private readonly IPhaseChallengeService _phaseChallengeService;

        public PhaseChallengeController(IPhaseChallengeService phaseChallengeService)
        {
            _phaseChallengeService = phaseChallengeService;
        }

        [HttpPost("{hackathonId}/assign-random-challenges")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRandomChallenges(int hackathonId, [FromQuery] int perPhase = 1)
        {
            try
            {
                var result = await _phaseChallengeService.AssignRandomChallengesToHackathonPhasesAsync(hackathonId, perPhase);
                return Ok(new { success = result, message = "Challenges assigned successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
