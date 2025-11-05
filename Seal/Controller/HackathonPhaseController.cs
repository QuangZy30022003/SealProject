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
            if (phase == null)
                return NotFound(new { message = "Không tìm thấy phase." });

            return Ok(phase);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateMultiple([FromBody] List<HackathonPhaseCreateDto> phases)
        {
            var createdPhases = new List<HackathonPhaseDto>();
            foreach (var dto in phases)
            {
                var created = await _service.CreateAsync(dto);
                createdPhases.Add(created);
            }
            return Ok(createdPhases);
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] HackathonPhaseUpdateDto dto)
        {
            try
            {
                var success = await _service.UpdateAsync(id, dto);
                if (!success)
                    return NotFound(new { message = "Không tìm thấy phase để cập nhật." });

                return Ok(new { message = "Cập nhật phase thành công." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "Không tìm thấy phase để xóa." });

            return Ok(new { message = "Xóa phase thành công." });
        }
    }
}
