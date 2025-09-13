using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface IChapterRepository: IRepository<Chapter>
    {
        Task<bool> ExistsByNameAsync(string name);
    }
}
