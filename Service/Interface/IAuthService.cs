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
        Task<User?> GetUserByIdAsync(int userId);
    }
}

