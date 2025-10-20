using Common.DTOs.CriterionDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class CriterionController : ControllerBase
    {
        private readonly ICriterionService _criterionService;

        public CriterionController(ICriterionService criterionService)
        {
            _criterionService = criterionService;
        }

        [HttpGet("Criterion/{phaseChallengeId}")]
        [Authorize]
        public async Task<IActionResult> GetByPhaseChallenge(int phaseChallengeId)
        {
            var result = await _criterionService.GetByPhaseChallengeAsync(phaseChallengeId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _criterionService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CriterionCreateDTO dto)
        {
            var result = await _criterionService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.CriteriaId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CriterionUpdateDTO dto)
        {
            dto.CriteriaId = id;
            var success = await _criterionService.UpdateAsync(dto);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _criterionService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var result = await _criterionService.GetAllAsync();
            return Ok(result);
        }
    }
}
