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
    public class RoleRepository : IRoleRepository
    {
        private readonly SealDbContext _context;

        public RoleRepository(SealDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Role role)
        {
            await _context.Roles.AddAsync(role);
        }

        public async Task<IEnumerable<Role>> GetAllAsync()
        {
          return  await _context.Roles.ToListAsync();
        }

        public Task<Role?> GetByIdAsync(int id)
        {
           return _context.Roles.FirstOrDefaultAsync(r => r.RoleId == id);
        }

        public  void Remove(Role role)
        {
             _context.Roles.Remove(role);
        }
    }
}
