using Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.UnitOfWork
{
    public interface IUOW : IDisposable
    {
        IRoleRepository Roles { get; }
        Task<int> SaveChangesAsync();
    }
}
