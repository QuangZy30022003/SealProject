using Common.DTOs.TeamChallengeDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamChallengesController : ControllerBase
    {
        private readonly ITeamChallengeService _service;

        public TeamChallengesController(ITeamChallengeService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] TeamChallengeRegisterDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                var result = await _service.RegisterTeamAsync(dto, userId);
                return Ok(new
                {
                    success = true,
                    message = $"Team registered successfully for {result.Count()} phases!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
