using Repositories.Interface;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.UnitOfWork
{
    public class UOW : IUOW
    {
        private readonly SealDbContext _context;

        public IRoleRepository Roles { get; }
        public IAuthRepository Users { get; }
        public UOW(SealDbContext context, IRoleRepository roleRepository, IAuthRepository authRepository)
        {
            _context = context;
            Roles = roleRepository;
            Users = authRepository;
        }   

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
