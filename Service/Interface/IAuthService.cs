using Common.DTOs.AuthDto;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IAuthService
    {
        Task<(string accessToken, string refreshToken, bool isVerified)> LoginWithGoogleAsync(string email);
        Task<bool> LogoutAsync(int userId);
        Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshToken);

        Task<string> VerifyEmailAsync(string token);
        Task<UserResponseDto?> GetUserByIdAsync(int userId);

        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<bool> UpdateUserInfoAsync(int userId, UpdateUserDto dto);
        Task<bool> SetUserBlockedStatusAsync(int userId, bool isBlocked);

        Task<(string accessToken, string refreshToken, bool isVerified)> LoginWithGoogleAsyncs(string email, string fullName);
    }
}

