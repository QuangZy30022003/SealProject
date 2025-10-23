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
    public class PhaseChallengeRepository : GenericRepository<PhaseChallenge>, IPhaseChallengeRepository
    {
        private readonly SealDbContext _context;

        public PhaseChallengeRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<int>> GetUsedChallengeIdsAsync()
        {
            return await _context.PhaseChallenges
                .Select(pc => pc.ChallengeId)
                .Distinct()
                .ToListAsync();
        }
    }
}
