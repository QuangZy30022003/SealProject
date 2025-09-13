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
    public class SeasonRepository : GenericRepository<Season>, ISeasonRepository
    {
        private readonly SealDbContext _context;

        public SeasonRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Season?> GetByCodeAsync(string code)
        {
            return await _context.Seasons
                .FirstOrDefaultAsync(s => s.Code == code);
        }
    }

}
