using AutoMapper;
using CloudinaryDotNet.Core;
using Common.DTOs.AuthDto;
using Common.Helper;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        public AuthService(IUOW uow, IEmailService emailService, JwtHelper jwtHelper, IMapper mapper, IConfiguration configuration)
        {
            _uow = uow;
            _emailService = emailService;
            _jwtHelper = jwtHelper;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<(string accessToken, string refreshToken, bool isVerified)> LoginWithGoogleAsync(string email)
        {
            var user = await _uow.Users.FirstOrDefaultAsync(u => u.Email == email);

            // ❗Nếu user tồn tại mà bị block => không cho login
            if (user != null && user.IsBlocked)
                throw new UnauthorizedAccessException("Your account has been blocked by the administrator.");


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
                    RefreshTokenExpiryTime = null,
                    IsBlocked = false
                };

                await _uow.Users.AddAsync(user);
                await _uow.SaveAsync();

                var verificationLink = $"https://sealfall25.somee.com/api/auth/verify?token={user.Token}";
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

        public async Task<string> VerifyEmailAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Invalid token.");

            var user = await _uow.Users.FirstOrDefaultAsync(u => u.Token == token);
            if (user == null)
                throw new KeyNotFoundException("User not found or token invalid.");

            if (user.IsVerified)
                return "Your account is already verified.";

            user.IsVerified = true;
            user.Token = null;

            _uow.Users.Update(user);
            await _uow.SaveAsync();

            return "Email verified successfully. You can now log in.";
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int userId)
        {
            var user = await _uow.Users.GetByIdIncludingAsync(u => u.UserId == userId, u => u.Role);
            if (user == null) return null;
            return _mapper.Map<UserResponseDto>(user);
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _uow.Users.GetAllAsync(includeProperties: "Role");
            return _mapper.Map<IEnumerable<UserResponseDto>>(users);
        }

        public async Task<bool> UpdateUserInfoAsync(int userId, UpdateUserDto dto)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return false;

            if (user.IsBlocked)
                throw new UnauthorizedAccessException("Your account has been blocked by admin.");

            user.FullName = dto.FullName;
            _uow.Users.Update(user);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<bool> SetUserBlockedStatusAsync(int userId, bool isBlocked)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return false;

            user.IsBlocked = isBlocked;
            _uow.Users.Update(user);
            await _uow.SaveAsync();
            return true;
        }


        public async Task<(string accessToken, string refreshToken, bool isVerified)> LoginWithGoogleAsyncs(string idToken)
        {
            // 1️⃣ Lấy clientId từ cấu hình
            var clientId = _configuration["Authentication:Google:ClientId"];

            // 2️⃣ Xác thực id_token từ Google
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException($"Invalid Google token: {ex.Message}");
            }

            // 3️⃣ Lấy thông tin từ payload
            var email = payload.Email;
            var fullName = payload.Name ?? email.Split('@')[0];
            var picture = payload.Picture;

            // 4️⃣ Tìm user theo email
            var user = await _uow.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null && user.IsBlocked)
                throw new UnauthorizedAccessException("Your account has been blocked by the administrator.");

            // 5️⃣ Nếu chưa có user thì tạo mới
            if (user == null)
            {
                user = new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = string.Empty,
                    RoleId = 1,
                    CreatedAt = DateTime.UtcNow,
                    IsVerified = true,
                    Token = null,
                    RefreshToken = null,
                    RefreshTokenExpiryTime = null,
                    IsBlocked = false
                };

                await _uow.Users.AddAsync(user);
                await _uow.SaveAsync();
            }

            // 6️⃣ Tạo access + refresh token
            var accessToken = _jwtHelper.GenerateToken(user);
            var refreshToken = _jwtHelper.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _uow.Users.Update(user);
            await _uow.SaveAsync();

            // 7️⃣ Trả kết quả
            return (accessToken, refreshToken, user.IsVerified);
        }
        public async Task<PartnerProfileBasicDto?> GetByUserIdAsync(int userId)
        {
            var entity = await _uow.PartnerProfiles
                                   .FirstOrDefaultAsync(p => p.UserId == userId);

            if (entity == null)
                return null;

            return new PartnerProfileBasicDto
            {
                ProfileId = entity.PartnerProfileId,
                CompanyName = entity.CompanyName,
                Description = entity.Description,
                Website = entity.Website,
                LogoUrl = entity.LogoUrl
            };
        }
        public async Task<bool> AdminUpdateUserAsync(int userId, UpdateUserDto dto)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return false;

            if (!string.IsNullOrEmpty(dto.FullName))
                user.FullName = dto.FullName;

            _uow.Users.Update(user);
            await _uow.SaveAsync();

            return true;
        }
        public async Task<bool> ChangeUserRoleAsync(int userId, int newRoleId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return false;

            var role = await _uow.Roles.GetByIdAsync(newRoleId);
            if (role == null)
                throw new ArgumentException("Role not found");

            user.RoleId = newRoleId;

            _uow.Users.Update(user);
            await _uow.SaveAsync();

            return true;
        }

    }
}
