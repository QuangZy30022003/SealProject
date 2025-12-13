using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Dto.AuthDto
{
    public class GoogleLoginRequest
    {
        public string Email { get; set; }
     
    }

    public class GoogleLoginRequestDto
    {
        public string Token { get; set; } = string.Empty;
    }
    public class LoginRequestDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
