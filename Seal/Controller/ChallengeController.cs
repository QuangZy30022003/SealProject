using Common.DTOs.ChallengeDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChallengeController : ControllerBase
    {
        private readonly IChallengeService _service;

        public ChallengeController(IChallengeService service)
        {
            _service = service;
        }

        //[HttpGet]
        //public async Task<IActionResult> GetAll()
        //{
        //    var challenges = await _service.GetAllAsync();
        //    return Ok(challenges);
        //}

        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetById(int id)
        //{
        //    var challenge = await _service.GetByIdAsync(id);
        //    if (challenge == null) return NotFound();
        //    return Ok(challenge);
        //}

        //[Authorize(Roles = "Partner")]
        //[HttpPost("create")]
        //public async Task<IActionResult> Create([FromForm] ChallengeCreateUnifiedDto dto, IFormFile? file)
        //{
        //    var userId = int.Parse(User.FindFirst("UserId")!.Value);

        //    ChallengeDto result;

        //    if (file != null)
        //    {
        //        // Upload từ file
        //        result = await _service.CreateFromFileAsync(dto, file, userId);
        //    }
        //    else if (!string.IsNullOrEmpty(dto.FilePath))
        //    {
        //        // Upload từ link
        //        result = await _service.CreateFromLinkAsync(dto, userId);
        //    }
        //    else
        //    {
        //        return BadRequest("Cần cung cấp file hoặc link.");
        //    }

        //    return Ok(result);
        //}

        //[Authorize(Roles = "Partner")]
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var userIdClaim = User.FindFirst("UserId")?.Value;
        //    if (userIdClaim == null) return Unauthorized("Không tìm thấy UserId trong token");
        //    int userId = int.Parse(userIdClaim);
        //    var result = await _service.PartnerDeleteAsync(id, userId);
        //    if (!result) return NotFound();
        //    return NoContent();
        //}

        //[Authorize(Roles = "Admin")]
        //[HttpPatch("{id}/status")]
        //public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChallengeStatusDto statusDto)
        //{
        //    var success = await _service.ChangeStatusAsync(id, statusDto);
        //    if (!success) return NotFound();
        //    return NoContent();
        //}
        //[Authorize(Roles = "Partner")]
        //[HttpPut("{id}/partner")]
        //public async Task<IActionResult> PartnerUpdate(int id, [FromForm] ChallengePartnerUpdateDto dto)
        //{
        //    var userIdClaim = User.FindFirst("UserId")?.Value;
        //    if (userIdClaim == null)
        //        return Unauthorized("Không tìm thấy UserId trong token");

        //    int userId = int.Parse(userIdClaim);

        //    var errorMessage = await _service.PartnerUpdateAsync(id, userId, dto);
        //    if (errorMessage != null)
        //        return BadRequest(new { message = errorMessage });

        //    return NoContent();
        //}

    }
}
