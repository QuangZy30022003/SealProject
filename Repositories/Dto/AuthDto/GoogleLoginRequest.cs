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
        public string FullName { get; set; }
    }
}
