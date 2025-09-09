using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class RoleService : IRoleService
    {
        private readonly IUOW _uow;
        public RoleService(IUOW uow)
        {
            _uow = uow;
        }
        public async Task<Role> CreateRoleAsync(string roleName)
        {
           if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException("Role name cannot be null or empty.", nameof(roleName));
            }

           var existingRoles = await _uow.Roles.GetAllAsync();
            if (existingRoles.Any(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Role '{roleName}' already exists.");
            }
            var newRole = new Role { RoleName = roleName };
            await _uow.Roles.AddAsync(newRole);
            await _uow.SaveAsync();
            return newRole;
        }
        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await _uow.Roles.GetAllAsync();
        }
    }
}
