using Common.DTOs.PartnerProfileDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/partner/profile")]
    public class PartnerProfileController : ControllerBase
    {
        private readonly IPartnerProfileService _service;

        public PartnerProfileController(IPartnerProfileService service)
        {
            _service = service;
        }

        private int GetUserIdFromToken()
        {
            return int.Parse(User.FindFirst("UserId")!.Value);
        }

        // GET: profile của chính partner
        [HttpGet]
        [Authorize(Roles = "Partner,Admin")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetUserIdFromToken();
            var result = await _service.GetByUserIdAsync(userId);
            return Ok(result);
        }

        // GET ALL - ADMIN
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        // CREATE
        [HttpPost]
        [Authorize(Roles = "Partner")]
        public async Task<IActionResult> Create([FromForm] PartnerProfileCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            try
            {
                var result = await _service.CreateAsync(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return clean readable message
                return BadRequest(new { message = ex.Message });
            }
        }

        // UPDATE
        [HttpPut]
        [Authorize(Roles = "Partner")]
        public async Task<IActionResult> Update([FromForm] PartnerProfileUpdateDto dto)
        {
            int userId = GetUserIdFromToken();
            var result = await _service.UpdateAsync(userId, dto);
            return Ok(result);
        }

        // DELETE
        [HttpDelete]
        [Authorize(Roles = "Partner")]
        public async Task<IActionResult> Delete()
        {
            var userId = GetUserIdFromToken();
            bool success = await _service.DeleteAsync(userId);

            return success
                ? Ok("Deleted")
                : NotFound("Profile not found");
        }

        // ADMIN GET PROFILE BY USER ID
        [HttpGet("user/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetById(int id)
        {
            try
            {
                var result = await _service.AdminGetByUserIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new List<object>());
            }
        }

    }
}