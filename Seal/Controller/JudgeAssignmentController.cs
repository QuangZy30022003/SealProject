using Common.DTOs.JudgeAssignmentDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class JudgeAssignmentController : ControllerBase
    {
        private readonly IJudgeAssignmentService _service;

        public JudgeAssignmentController(IJudgeAssignmentService service)
        {
            _service = service;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Adminassignjugde")]
        public async Task<IActionResult> AssignJudge([FromBody] JudgeAssignmentCreateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")!.Value); // adminId từ JWT
                var result = await _service.AssignJudgeAsync(dto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("hackathon/{hackathonId}")]
        public async Task<IActionResult> GetByHackathon(int hackathonId)
        {
            var result = await _service.GetByHackathonAsync(hackathonId);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("block/{assignmentId}")]
        public async Task<IActionResult> BlockAssignment(int assignmentId)
        {
            try
            {
                var result = await _service.RemoveAssignmentAsync(assignmentId);
                if (result)
                    return Ok(new { message = "Judge assignment has been blocked successfully." });

                return BadRequest(new { message = "Failed to block judge assignment." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("reactivate/{assignmentId}")]
        public async Task<IActionResult> ReactivateAssignment(int assignmentId)
        {
            try
            {
                await _service.ReactivateAssignmentAsync(assignmentId);
                return Ok("Assignment reactivated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}