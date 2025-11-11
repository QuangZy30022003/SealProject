using Common;
using Common.DTOs.ChapterDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChapterController : ControllerBase
    {
        private readonly IChapterService _chapterService;
        private readonly IUserContextService _userContext;

        public ChapterController(IChapterService chapterService, IUserContextService userContext)
        {
            _chapterService = chapterService;
            _userContext = userContext;
        }

        [Authorize(Roles = "ChapterLeader")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateChapterDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var userId = _userContext.GetCurrentUserId();
                var result = await _chapterService.CreateChapterAsync(dto, userId);
                return CreatedAtAction(nameof(GetById), new { id = result.ChapterId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById([FromRoute] int id)
        {
            var result = await _chapterService.GetByIdAsync(id);
            return result is null ? NotFound(new { message = "Chapter not found" }) : Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _chapterService.GetAllAsync();
            return Ok(result);
        }
        [Authorize(Roles = "ChapterLeader")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateChapterDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _chapterService.UpdateAsync(id, dto);
            if (result == null)
                return NotFound(new { message = "Chapter not found" });

            return Ok(result);
        }
        [Authorize(Roles = "ChapterLeader")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userContext.GetCurrentUserId();

            try
            {
                var success = await _chapterService.DeleteAsync(id, userId);
                return success
                    ? Ok(new { message = "Chapter deleted successfully" })
                    : NotFound(new { message = "Chapter not found" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }
    }
}
