using Common;
using Common.DTOs.AppealDto;
using Common.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppealController : ControllerBase
    {
        private readonly IAppealService _appealService;
        private readonly IUserContextService _userContext;

        public AppealController(IAppealService appealService, IUserContextService userContext)
        {
            _appealService = appealService;
            _userContext = userContext;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateAppeal(CreateAppealDto dto)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var result = await _appealService.CreateAppealAsync(dto, userId);
                return Ok(ApiResponse<AppealResponseDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [Authorize]
        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetAppealsByTeam(int teamId)
        {
            var result = await _appealService.GetAppealsByTeamAsync(teamId);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllAppeals()
        {
            var result = await _appealService.GetAllAppealsAsync();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{appealId}")]
        public async Task<IActionResult> GetById(int appealId)
        {
            var result = await _appealService.GetAppealByIdAsync(appealId);
            if (result == null)
                return NotFound(new { message = "Appeal not found." });

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{appealId}/review")]
        public async Task<IActionResult> Review(int appealId, ReviewAppealDto dto)
        {
            try
            {
                var reviewerId = _userContext.GetCurrentUserId();
                var result = await _appealService.ReviewAppealAsync(appealId, dto, reviewerId);

                if (result == null)
                    return NotFound(ApiResponse<object>.Fail("Appeal not found."));

                return Ok(ApiResponse<AppealResponseDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [Authorize]
        [HttpGet("phase/{phaseId}")]
        public async Task<IActionResult> GetAppealsByPhase(int phaseId)
        {
            try
            {
                var result = await _appealService.GetAppealsByPhaseAsync(phaseId);
                return Ok(ApiResponse<IEnumerable<AppealResponseDto>>.Ok(result, "Appeals retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [Authorize]
        [HttpGet("team/{teamId}/phase/{phaseId}")]
        public async Task<IActionResult> GetAppealsByTeamAndPhase(int teamId, int phaseId)
        {
            var result = await _appealService.GetAppealsByTeamAndPhaseAsync(teamId, phaseId);
            return Ok(result);
        }

    }
}
