using Common;
using Common.DTOs.TeamDto;
using Common.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly IUserContextService _userContext;

        public TeamController(ITeamService teamService, IUserContextService userContext)
        {
            _teamService = teamService;
            _userContext = userContext;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = _userContext.GetCurrentUserId();

            try
            {
                var team = await _teamService.CreateTeamAsync(dto, userId);
                return CreatedAtAction(nameof(GetById), new { id = team.TeamId }, team);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById([FromRoute] int id)
        {
            var result = await _teamService.GetByIdAsync(id);
            return result is null
                ? NotFound(new { message = "Team not found" })
                : Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _teamService.GetAllAsync();
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTeamDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var result = await _teamService.UpdateAsync(id, dto);
                return result == null
                    ? NotFound(new { message = "Team not found" })
                    : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userContext.GetCurrentUserId();

            try
            {
                var success = await _teamService.DeleteAsync(id, userId);
                return success
                    ? Ok(new { message = "Team deleted successfully" })
                    : NotFound(new { message = "Team not found" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpGet("chapter/{chapterId}")]
        public async Task<IActionResult> GetTeamsByChapter(int chapterId)
        {
            var result = await _teamService.GetTeamsByChapterIdAsync(chapterId);
            return Ok(result);
        }

        [HttpGet("{phaseId}/teams")]
        public async Task<IActionResult> GetTeamsByPhase(int phaseId)
        {
            try
            {
                var teams = await _teamService.GetTeamsByPhaseIdAsync(phaseId);
                return Ok(ApiResponse<object>.Ok(teams, "Teams retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [Authorize]
        [HttpGet("my-teams")]
        public async Task<IActionResult> GetMyTeams()
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var teams = await _teamService.GetUserTeamsAsync(userId);
                return Ok(ApiResponse<object>.Ok(teams, "User teams retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }
    }
}
