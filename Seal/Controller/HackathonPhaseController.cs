using Common.DTOs.HackathonPhaseDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class HackathonPhaseController : ControllerBase
    {
        private readonly IHackathonPhaseService _service;

        public HackathonPhaseController(IHackathonPhaseService service)
        {
            _service = service;
        }

        [HttpGet("{hackathonId}")]
        public async Task<IActionResult> GetByHackathon(int hackathonId)
        {
            var phases = await _service.GetPhasesByHackathonAsync(hackathonId);
            return Ok(phases);
        }

        [HttpGet("phase/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var phase = await _service.GetByIdAsync(id);
            if (phase == null) return NotFound();
            return Ok(phase);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HackathonPhaseCreateDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.PhaseId }, created);
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] HackathonPhaseUpdateDto dto)
        {
            var success = await _service.UpdateAsync(id, dto);
            return success ? Ok() : NotFound();
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }
    }
}
