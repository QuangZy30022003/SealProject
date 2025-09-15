using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            // Nếu có claim "UserId"
            var userIdClaim = user?.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                throw new UnauthorizedAccessException("UserId claim not found in token");
            }

            return int.Parse(userIdClaim.Value);
        }

        public string? GetCurrentUserEmail()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Email)?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public string? GetCurrentRoleId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst("RoleId")?.Value;
        }
    }
}
