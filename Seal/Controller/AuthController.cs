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
            try
            {
                var result = await _authService.VerifyEmailAsync(token);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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
