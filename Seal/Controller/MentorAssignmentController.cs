using Common.DTOs.AssignedTeamDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class MentorAssignmentController : ControllerBase
    {
        private readonly IMentorAssignmentService _service;

        public MentorAssignmentController(IMentorAssignmentService service)
        {
            _service = service;
        }


        [HttpGet("mentor/{mentorId}/assignments")]
        public async Task<IActionResult> GetAssignedTeams(int mentorId)
        {
            var result = await _service.ViewAssignedTeamsAsync(mentorId);
            if (!result.Any())
                return NotFound(new { message = "No teams assigned to this mentor" });

            return Ok(result);
        }

        [HttpPost]
        [Authorize] // chỉ cần login
        public async Task<IActionResult> Register([FromBody] MentorAssignmentCreateDto dto)
        {
            int userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            var result = await _service.RegisterAsync(userId, dto);
            return Ok(result);
        }


        [HttpPut("{assignmentId}/approve")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> Approve(int assignmentId)
        {
            var result = await _service.ApproveAsync(assignmentId);
            return Ok(result);
        }

        [HttpPut("{assignmentId}/reject")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> Reject(int assignmentId)
        {
            var result = await _service.RejectAsync(assignmentId);
            return Ok(result);
        }

        [HttpGet("mentor/{mentorId}/teams")]
        public async Task<IActionResult> GetByMentor(int mentorId)
        {
            var result = await _service.GetByMentorAsync(mentorId);
            return Ok(result);
        }
    }
}

