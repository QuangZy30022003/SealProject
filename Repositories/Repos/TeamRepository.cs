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
    public class TeamRepository : GenericRepository<Team>, ITeamRepository
    {
        private readonly SealDbContext _context;

        public TeamRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByNameAsync(string teamName, int? chapterId)
        {
            return await _context.Teams
                .AnyAsync(t => t.TeamName == teamName && t.ChapterId == chapterId);
        }
    }
}
