using Common.DTOs.AppealDto;
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

        public AppealController(IAppealService appealService)
        {
            _appealService = appealService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppeal([FromBody] CreateAppealDto dto)
        {
            try
            {
                var result = await _appealService.CreateAppealAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetAppealsByTeam(int teamId)
        {
            var result = await _appealService.GetAppealsByTeamAsync(teamId);
            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllAppeals()
        {
            var result = await _appealService.GetAllAppealsAsync();
            return Ok(result);
        }

        [HttpGet("{appealId}")]
        public async Task<IActionResult> GetById(int appealId)
        {
            var result = await _appealService.GetAppealByIdAsync(appealId);
            if (result == null)
                return NotFound(new { message = "Appeal not found." });

            return Ok(result);
        }

        [HttpPut("{appealId}/review")]
        public async Task<IActionResult> ReviewAppeal(int appealId, [FromBody] ReviewAppealDto dto)
        {
            try
            {
                var result = await _appealService.ReviewAppealAsync(appealId, dto);
                if (result == null)
                    return NotFound(new { message = "Appeal not found." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
