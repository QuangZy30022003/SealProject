using Common.Helper;
using Microsoft.AspNetCore.Mvc;
using Repositories.Dto.AuthDto;
using Repositories.UnitOfWork;
using Service.Interface;
using Service.Servicefolder;
using System.IdentityModel.Tokens.Jwt;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUOW _uow;
        private readonly JwtHelper _jwtHelper;
        private readonly IAuthService _authService;
        public AuthController(IUOW uow, JwtHelper jwtHelper, IAuthService authService)
        {
            _uow = uow;
            _jwtHelper = jwtHelper;
            _authService = authService;
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Invalid token.");

            var user = await _uow.AuthRepository.GetByTokenAsync(token); // bạn cần thêm method này trong UserRepo
            if (user == null)
                return NotFound("User not found or token invalid.");

            if (user.IsVerified)
                return Ok("Your account is already verified.");

            // cập nhật trạng thái
            user.IsVerified = true;
            user.Token = null; // clear token sau khi xác thực
            await _uow.SaveAsync();

            return Ok("Email verified successfully. You can now log in.");
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] string email)
        {
            var (accessToken, refreshToken, isVerified) = await _authService.LoginWithGoogleAsync(email);

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IsVerified = isVerified
            });
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            var userId = int.Parse(userIdClaim.Value);

            var result = await _authService.LogoutAsync(userId);

            if (!result)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "Logout successful" });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            try
            {
                var (accessToken, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
                return Ok(new { AccessToken = accessToken, RefreshToken = newRefreshToken });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
