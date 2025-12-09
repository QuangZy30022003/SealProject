using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class QualificationController : ControllerBase
    {
        private readonly IQualificationService _qualificationService;
        public QualificationController(IQualificationService qualificationService)
        {
            _qualificationService = qualificationService;
        }
        // ✅ GET: Lấy danh sách các đội đã đủ điều kiện vào vòng tiếp theo
        [Authorize(Roles = "Admin")]
        [HttpPost("phase/{phaseId}/top-teams")]
        public async Task<IActionResult> GetTopTeams(int phaseId)
        {
            var result = await _qualificationService.GenerateQualifiedTeamsAsync(phaseId);
            return Ok(result);
        }
        [Authorize]
        [HttpGet("{phaseId}/final-qualified")]
        public async Task<IActionResult> GetFinalQualifiedTeams(int phaseId)
        {
            try
            {
                var result = await _qualificationService.GetFinalQualifiedTeamsAsync(phaseId);
                return Ok(result);
            }
            catch (ArgumentException )
            {
                return Ok(new List<object>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred", detail = ex.Message });
            }
        }

    }
}

