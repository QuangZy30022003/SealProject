using Common.DTOs.ScheduleEventDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleEventController : ControllerBase
    {
        private readonly IScheduleEventService _service;

        public ScheduleEventController(IScheduleEventService service)
        {
            _service = service;
        }

        [HttpGet("hackathon/{hackathonId}")]
        public async Task<IActionResult> GetByHackathon(int hackathonId)
        {
            try
            {
                var events = await _service.GetEventsByHackathonAsync(hackathonId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("phase/{phaseId}")]
        public async Task<IActionResult> GetByPhase(int phaseId)
        {
            try
            {
                var events = await _service.GetEventsByPhaseAsync(phaseId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var scheduleEvent = await _service.GetByIdAsync(id);
                if (scheduleEvent == null)
                    return NotFound(new { message = "Schedule event not found." });

                return Ok(scheduleEvent);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ScheduleEventCreateDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.EventId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the schedule event.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ScheduleEventUpdateDto dto)
        {
            try
            {
                var success = await _service.UpdateAsync(id, dto);
                if (!success)
                    return NotFound(new { message = "Schedule event not found." });

                return Ok(new { message = "Schedule event updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the schedule event.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _service.DeleteAsync(id);
                if (!success)
                    return NotFound(new { message = "Schedule event not found." });

                return Ok(new { message = "Schedule event deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the schedule event.", detail = ex.Message });
            }
        }
    }
}
