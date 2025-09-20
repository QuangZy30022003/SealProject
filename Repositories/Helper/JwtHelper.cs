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

namespace Common.Helper
{
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //    public string GenerateToken(User user)
        //    {
        //        var jwtSettings = _configuration.GetSection("JwtSettings");

        //        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
        //        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //        var claims = new[]
        //        {
        //            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        //new Claim("UserId", user.UserId.ToString()),
        //new Claim("RoleId", user.RoleId?.ToString() ?? "0"),
        //new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        //        };

        //        var token = new JwtSecurityToken(
        //            issuer: jwtSettings["Issuer"],
        //            audience: jwtSettings["Audience"],
        //            claims: claims,
        //            expires: DateTime.UtcNow.AddHours(2),
        //            signingCredentials: creds
        //        );

        //        return new JwtSecurityTokenHandler().WriteToken(token);
        //    }

        public string GenerateToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim("UserId", user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            // 🔑 Add Role Claim
            if (user.RoleId.HasValue)
            {
                string roleName = user.RoleId switch
                {
                    1 => "Member",
                    2 => "Admin",
                    3 => "TeamLeader",
                    4 => "Partner"
                };

                claims.Add(new Claim(ClaimTypes.Role, roleName));
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
