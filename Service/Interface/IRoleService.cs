using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IRoleService
    {
        Task<Role> CreateRoleAsync(string roleName);
        Task<IEnumerable<Role>> GetAllRolesAsync();
    }
}
