using Common.DTOs.ChapterDto;
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

        public ChapterController(IChapterService chapterService)
        {
            _chapterService = chapterService;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateChapterDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var result = await _chapterService.CreateChapterAsync(dto);
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
            return result is null ? NotFound() : Ok(result);
        }
    }
}
