using Common.Helper;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class AuthService : IAuthService
    {
        private readonly IUOW _uow;
        private readonly IEmailService _emailService;
        private readonly JwtHelper _jwtHelper;
        public AuthService(IUOW uow, IEmailService emailService, JwtHelper jwtHelper)
        {
            _uow = uow;
            _emailService = emailService;
            _jwtHelper = jwtHelper;
        }

        public async Task<(string accessToken, string refreshToken, bool isVerified)> LoginWithGoogleAsync(string email)
        {
            var user = await _uow.AuthRepository.GetByEmailAsync(email);

            if (user == null)
            {
                var fullName = email.Split('@')[0];

                user = new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = string.Empty,
                    RoleId = 1,
                    CreatedAt = DateTime.UtcNow,
                    IsVerified = false,
                    Token = Guid.NewGuid().ToString(),
                    RefreshToken = null,
                    RefreshTokenExpiryTime = null
                };

                await _uow.Users.AddAsync(user);
                await _uow.SaveAsync();

                var verificationLink = $"https://localhost:7268/api/auth/verify?token={user.Token}";
                var subject = "Verify your email";
                var body = $"<p>Hello {user.FullName},</p>" +
                           $"<p>Please verify your account by clicking the link below:</p>" +
                           $"<a href='{verificationLink}'>Verify Email</a>";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }

            // Sinh Access Token
            var accessToken = _jwtHelper.GenerateToken(user);

            // Sinh Refresh Token (random string)
            var refreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            // Cập nhật DB
            _uow.Users.Update(user);
            await _uow.SaveAsync();

            return (accessToken, refreshToken, user.IsVerified);
        }



        public async Task<bool> LogoutAsync(int userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            _uow.Users.Update(user);
            await _uow.SaveAsync();

            return true;
        }

        public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshToken)
        {
            var user = (await _uow.Users.GetAllAsync(u => u.RefreshToken == refreshToken)).FirstOrDefault();

            if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid refresh token");

            // Create new access token
            var newAccessToken = _jwtHelper.GenerateToken(user);

            // tạo refresh token mới mỗi lần refresh 
            var newRefreshToken = _jwtHelper.GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _uow.Users.Update(user);
            await _uow.SaveAsync();

            return (newAccessToken, newRefreshToken);
        }
    }
}
