using Common.DTOs.HackathonDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class HackathonsController : ControllerBase
    {
        private readonly IHackathonService _service;

        public HackathonsController(IHackathonService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] HackathonCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var created = await _service.CreateHackathonAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.HackathonId }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] HackathonCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var updated = await _service.UpdateHackathonAsync(id, dto, userId);
            return updated == null ? NotFound() : Ok(updated);
        }
     
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
