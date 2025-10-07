using Microsoft.EntityFrameworkCore;
using Repositories.Interface;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos
{
    public class StudentVerificationRepository : GenericRepository<StudentVerification>, IStudentVerificationRepository
    {
        private readonly SealDbContext _context;
        public StudentVerificationRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<StudentVerification?> GetByUserIdAsync(int userId)
        {
            return await _context.StudentVerifications.FirstOrDefaultAsync(x => x.UserId == userId);
        }

    }
}
