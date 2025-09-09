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
    public class AuthRepository : GenericRepository<User>, IAuthRepository
    {
        private readonly SealDbContext _context;

        public AuthRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByTokenAsync(string token)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Token == token);
        }
    }
}
