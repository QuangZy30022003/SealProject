using Common.DTOs.StudentVerification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Servicefolder;
using System.Security.Claims;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentVerificationController : ControllerBase
    {
        private readonly IStudentVerificationService _service;

        public StudentVerificationController(IStudentVerificationService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromForm] StudentVerificationDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

            if (userIdClaim == null || emailClaim == null)
                return Unauthorized("Invalid token");

            var userId = int.Parse(userIdClaim.Value);
            var email = emailClaim.Value;

            await _service.SubmitAsync(userId, email, dto);
            return Ok(new { message = "Yêu cầu xác thực đã được gửi." });
        }

        [HttpPut("approve/{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var result = await _service.ApproveVerificationAsync(id);
                if (!result)
                    return NotFound("Không tìm thấy xác thực sinh viên.");
                return Ok("Phê duyệt xác thực thành công.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("reject/{id}")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var result = await _service.RejectVerificationAsync(id);
                if (!result)
                    return NotFound("Không tìm thấy xác thực sinh viên.");
                return Ok("Từ chối xác thực thành công.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
