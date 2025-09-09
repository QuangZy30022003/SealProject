using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface ITeamRepository: IRepository<Team>
    {
        Task<bool> ExistsByNameAsync(string teamName, int? chapterId);
    }
}
