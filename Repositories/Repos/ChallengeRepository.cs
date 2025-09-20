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
    public class ChallengeRepository : GenericRepository<Challenge>, IChallengeRepository
    {
        private readonly SealDbContext _context;

        public ChallengeRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Challenge>> GetChallengesWithSeasonAndUserAsync()
        {
            return await _context.Challenges
                .Include(c => c.Season)
                .Include(c => c.User)
                .ToListAsync();
        }
    }
}
