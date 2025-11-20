using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
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
        public async Task<IActionResult> GetTopTeams(int phaseId, [FromQuery] int quantity)
        {
            var result = await _qualificationService.GenerateQualifiedTeamsAsync(phaseId, quantity);
            return Ok(result);
        }

    }
}

