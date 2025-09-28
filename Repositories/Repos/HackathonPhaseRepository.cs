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
    public class HackathonPhaseRepository : GenericRepository<HackathonPhase>, IHackathonPhaseRepository
    {
        private readonly SealDbContext _context;

        public HackathonPhaseRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<HackathonPhase>> GetByHackathonIdAsync(int hackathonId)
        {
            return await _context.HackathonPhases
                .Where(p => p.HackathonId == hackathonId)
                .OrderBy(p => p.StartDate)
                .ToListAsync();
        }
    }
}
