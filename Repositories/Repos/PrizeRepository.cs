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
    public class PrizeRepository : GenericRepository<Prize>, IPrizeRepository
    {
        private readonly SealDbContext _context;

        public PrizeRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Prize>> GetPrizesByHackathonIdAsync(int hackathonId)
        {
            return await _context.Set<Prize>()
                .Include(p => p.Hackathon)
                .Where(p => p.HackathonId == hackathonId)
                .ToListAsync();
        }
    }
}
