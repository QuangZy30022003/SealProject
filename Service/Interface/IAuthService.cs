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
        Task<(User user, bool isVerified)> LoginWithGoogleAsync(string email, string fullName);
    }
}
