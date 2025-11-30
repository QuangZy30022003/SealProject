using Common.DTOs.ChallengeDto;
using Common.DTOs.TrackDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackController : ControllerBase
    {
        private readonly ITrackService _trackService;

        public TrackController(ITrackService trackService)
        {
            _trackService = trackService;
        }

        // ✅ GET: api/track
        [HttpGet]
        public async Task<IActionResult> GetAllTracks()
        {
            var result = await _trackService.GetTracksdAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrackById(int id)
        {
            var result = await _trackService.GetTrackByIdAsync(id);
            if (result == null)
                return NotFound("Track not found");
            return Ok(result);
        }
        // ✅ POST: api/track
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateTrack([FromBody] CreateTrackDto dto)
        {
            var result = await _trackService.CreateTrackAsync(dto);
            return CreatedAtAction(nameof(GetAllTracks), new { id = result.TrackId }, result);
        }

        // ✅ PUT: api/track/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTrack(int id, [FromBody] UpdateTrackDto dto)
        {
            var result = await _trackService.UpdateTrackAsync(id, dto);
            if (result == null)
                return NotFound("Track not found");

            return Ok(result);
        }

        // ✅ DELETE: api/track/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrack(int id)
        {
            var success = await _trackService.DeleteTrackAsync(id);
            if (!success)
                return NotFound("Track not found");

            return NoContent();
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("assign-random-challenge")]
        public async Task<IActionResult> AssignRandomChallenge([FromBody] RandomChallengeTrackRequest request)
        {
            var result = await _trackService.AssignRandomChallengesToTrackAsync(request);
            if (result == null) return BadRequest("No valid challenges or track not found");

            return Ok(result);
        }

        [HttpGet("phase/{phaseId}")]
        public async Task<IActionResult> GetTracksByPhase(int phaseId)
        {
            try
            {
                var result = await _trackService.GetTracksByPhaseIdAsync(phaseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
