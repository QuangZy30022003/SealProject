using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface ICriterionDetailRepository : IRepository<CriterionDetail>
    {
        Task<CriterionDetail?> GetByIdAsync(int id);
            }
}
