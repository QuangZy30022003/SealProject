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
        public async Task<IActionResult> GetHackathonDetail(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Hackathon not found" });

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] HackathonCreateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId").Value);
                var created = await _service.CreateHackathonAsync(dto, userId);
                return CreatedAtAction(nameof(GetHackathonDetail), new { id = created.HackathonId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] HackathonCreateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId").Value);
                var updated = await _service.UpdateHackathonAsync(id, dto, userId);
                return updated == null ? NotFound(new { message = "Hackathon not found" }) : Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }
     
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                    return BadRequest(new { message = "Status is required." });

                var validStatuses = new[] { "Pending", "InProgress", "Complete", "Unactive" };
                if (!validStatuses.Contains(dto.Status))
                    return BadRequest(new { message = "Invalid status." });

                var updated = await _service.UpdateStatusAsync(id, dto.Status);
                if (updated == null)
                    return NotFound(new { message = "Hackathon not found." });

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Không gửi detail ra production (ở dev có thể log)
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

    }
}
