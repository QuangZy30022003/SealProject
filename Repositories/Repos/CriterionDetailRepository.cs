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
    public class CriterionDetailRepository : GenericRepository<CriterionDetail>, ICriterionDetailRepository
    {
        private readonly DbContext _context;
        private readonly DbSet<CriterionDetail> _dbSet;

        public CriterionDetailRepository(DbContext context) : base(context)
        {
            _context = context;
            _dbSet = context.Set<CriterionDetail>();
        }

        public async Task<CriterionDetail?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(cd => cd.CriterionDetailId == id);
        }
    }
}
