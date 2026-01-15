using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Repositories.Models;
using System.Security.Cryptography;
using Repositories.UnitOfWork;

namespace Common.Helper
{
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;
        private readonly IUOW _iouw;

        public JwtHelper(IConfiguration configuration, IUOW iouw)
        {
            _configuration = configuration;
            _iouw = iouw;
        }

        public async Task<string> GenerateToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim("UserId", user.UserId.ToString()),
              new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            if (user.RoleId.HasValue)
            {
                var role = await _iouw.Roles.GetByIdAsync(user.RoleId.Value);
                if (role != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
                }
            }

            var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(2),
                    signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

    }
}
