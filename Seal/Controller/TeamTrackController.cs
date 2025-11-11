using Common.DTOs.TeamTrackDto;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamTrackController : ControllerBase
    {
        private readonly ITeamTrackService _teamTrackService;

        public TeamTrackController(ITeamTrackService teamTrackService)
        {
            _teamTrackService = teamTrackService;
        }

        [HttpPost("select")]
        public async Task<IActionResult> SelectTrack([FromBody] TeamSelectTrackRequest request)
        {
            try
            {
                // Lấy UserId từ token JWT
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (userIdClaim == null) return Unauthorized("Invalid token");

                int userIdFromToken = int.Parse(userIdClaim);

                // Gọi service để select track
                var result = await _teamTrackService.SelectTrackAsync(userIdFromToken, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}