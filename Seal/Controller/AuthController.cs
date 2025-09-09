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
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var (user, isVerified) = await _authService.LoginWithGoogleAsync(request.Email, request.FullName);

            if (!isVerified)
            {
                return Ok(new
                {
                    Message = "Please verify your email. A verification link has been sent.",
                    UserId = user.UserId,
                    Email = user.Email
                });
            }

            var token = _jwtHelper.GenerateToken(user);

            return Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                Token = token
            });
        }

    }
}
