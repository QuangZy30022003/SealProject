using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface ISeasonRepository : IRepository<Season>
    {
        Task<Season?> GetByCodeAsync(string code);
        Task<Season?> GetByNameAsync(string name);
        Task UpdateSeasonAsync(Season season);
    }
}
