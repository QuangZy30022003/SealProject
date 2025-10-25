using Common.DTOs.PrizeDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;
using System.Security.Claims;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PrizeController : ControllerBase
    {
        private readonly IPrizeService _service;

        public PrizeController(IPrizeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("hackathon/{hackathonId}")]
        public async Task<IActionResult> GetByHackathon(int hackathonId)
        {
            var result = await _service.GetByHackathonAsync(hackathonId);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreatePrizeDTO dto)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Không thể xác định UserId từ token.");
            }

            // Gọi service để tạo giải thưởng
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update([FromBody] UpdatePrizeDTO dto)
        {
            var result = await _service.UpdateAsync(dto);
            return Ok(result);
        }

        [HttpDelete("{prizeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int prizeId)
        {
            await _service.DeleteAsync(prizeId);
            return Ok(new { message = "Xóa giải thưởng thành công" });
        }
    }
}
