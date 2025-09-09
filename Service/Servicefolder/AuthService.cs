using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class AuthService : IAuthService
    {
        private readonly IUOW _uow;
        private readonly IEmailService _emailService;
        public AuthService(IUOW uow, IEmailService emailService)
        {
            _uow = uow;
            _emailService = emailService;
        }

        public async Task<(User user, bool isVerified)> LoginWithGoogleAsync(string email, string fullName)
        {
            var user = await _uow.AuthRepository.GetByEmailAsync(email);

            if (user == null)
            {
                user = new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = string.Empty,
                    RoleId = 1,
                    CreatedAt = DateTime.UtcNow,
                    IsVerified = false,
                    Token = Guid.NewGuid().ToString()
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

            return (user, user.IsVerified);
        }


    }
}
