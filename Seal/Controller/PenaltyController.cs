using Common;
using Common.DTOs.PenaltyBonusDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PenaltyController : ControllerBase
    {
        private readonly IPenaltyService _service;
        private readonly IUserContextService _userContext;

        public PenaltyController(IPenaltyService service, IUserContextService userContext)
        {
            _service = service;
            _userContext = userContext;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePenaltiesBonuseDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = _userContext.GetCurrentUserId();
            try
            {
                var result = await _service.CreateAsync(dto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("phase/{phaseId}")]
        public async Task<IActionResult> GetByPhase(int phaseId)
        {
            return Ok(await _service.GetByPhaseAsync(phaseId));
        }

        [HttpGet("team/{teamId}/phase/{phaseId}")]
        public async Task<IActionResult> GetByTeam(int teamId, int phaseId)
        {
            return Ok(await _service.GetByTeamAsync(teamId, phaseId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Adjustment not found." });

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePenaltiesBonuseDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto);
                if (result == null)
                    return NotFound(new { message = "Adjustment not found." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            bool ok = await _service.SoftDeleteAsync(id);
            if (!ok) return NotFound(new { message = "Adjustment not found." });
            return NoContent();
        }
    }
}
