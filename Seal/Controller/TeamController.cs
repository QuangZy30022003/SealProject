using Common.DTOs.TeamDto;
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

        public TeamController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
        {
            try
            {
                var team = await _teamService.CreateTeamAsync(dto);
                return Ok(team);
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
            return result is null ? NotFound() : Ok(result);
        }
    }
}
