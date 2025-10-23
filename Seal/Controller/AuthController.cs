using Common;
using Common.DTOs.AuthDto;
using Common.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Dto.AuthDto;
using Repositories.UnitOfWork;
using Service.Interface;
using Service.Servicefolder;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Seal.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUOW _uow;
        private readonly JwtHelper _jwtHelper;
        private readonly IAuthService _authService;
        private readonly IUserContextService _userContext;
        public AuthController(IUOW uow, JwtHelper jwtHelper, IAuthService authService, IUserContextService userContextService)
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

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            var userId = int.Parse(userIdClaim.Value);

            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }

        //[Authorize]
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            //var currentUserId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            //var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            //if (currentUserId != id && role != "Administrator")
            //    return Forbid();

            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }
        //[Authorize]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }

        [Authorize]
        [HttpPut("update-info/{id}")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateUserDto dto)
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Invalid token" });
                var userId = int.Parse(userIdClaim.Value);

                var result = await _authService.UpdateUserInfoAsync(userId, dto);
                if (!result) return NotFound(new { message = "User not found" });

                return Ok(new { message = "Profile updated successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        //[Authorize(Roles = "Administrator,Moderator")]
        [HttpPut("users/{id}/block")]
        public async Task<IActionResult> BlockUser(int id, [FromQuery] bool isBlocked)
        {
            var result = await _authService.SetUserBlockedStatusAsync(id, isBlocked);
            if (!result) return NotFound(new { message = "User not found" });

            string status = isBlocked ? "blocked" : "unblocked";
            return Ok(new { message = $"User {status} successfully" });
        }
    }
}
