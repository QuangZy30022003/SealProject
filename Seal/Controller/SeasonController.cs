using Common.DTOs.SeasonDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeasonController : ControllerBase
    {
        private readonly ISeasonService _seasonService;
        public SeasonController(ISeasonService seasonService)
        {
            _seasonService = seasonService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var seasons = await _seasonService.GetAllSeasonsAsync();
            return Ok(seasons);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var season = await _seasonService.GetByIdAsync(id);
            if (season == null) return NotFound();
            return Ok(season);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Create([FromBody] SeasonRequest dto)
        {
            var result = await _seasonService.CreateAsync(dto);
            if (result.Contains("exists"))
                return BadRequest(new { message = result });

            return Ok(new { message = result });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Update(int id, [FromBody] SeasonUpdateDto dto)
        {
            var result = await _seasonService.UpdateAsync(id, dto);
            if (result.Contains("not found") || result.Contains("exists") || result.Contains("must be earlier"))
                return BadRequest(new { message = result });

            return Ok(new { message = result });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _seasonService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
