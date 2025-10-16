using Common.DTOs.Submission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        // 🟢 Thành viên tạo bản nháp
        [HttpPost("create-draft")]
        public async Task<IActionResult> CreateDraft([FromBody] SubmissionCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value); // hoặc ClaimTypes.NameIdentifier
            var result = await _submissionService.CreateDraftSubmissionAsync(dto, userId);
            return Ok(new
            {
                success = true,
                message = "Draft submission created successfully!",
                data = result
            });
        }

        // 🟢 Leader chọn bản final
        [HttpPost("set-final")]
        public async Task<IActionResult> SetFinal([FromBody] SubmissionSelectFinalDto dto)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var result = await _submissionService.SetFinalSubmissionAsync(dto, userId);
            return Ok(new
            {
                success = true,
                message = "Final submission selected successfully!",
                data = result
            });
        }

        // 🟢 Lấy tất cả submission của team trong phase
        [HttpGet("submissions/team-phase")]
        [Authorize(Roles = "Admin,Judge")]
        public async Task<IActionResult> GetByTeamAndPhase([FromQuery] int? teamId, [FromQuery] int? phaseChallengeId)
        {
            var result = await _submissionService.GetSubmissionsByTeamAndPhaseAsync(teamId, phaseChallengeId);
            return Ok(new { success = true, data = result });
        }
    }
}