using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.UnitOfWork
{
    public interface IUOW : IDisposable
    {

        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
