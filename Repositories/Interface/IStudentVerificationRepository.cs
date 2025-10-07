using Repositories.Models;
using Repositories.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface IStudentVerificationRepository : IRepository<StudentVerification>  
    {
        Task<StudentVerification?> GetByUserIdAsync(int userId);

    }
}
