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
    public class MentorAssignmentRepository : GenericRepository<MentorAssignment>, IMentorAssignmentRepository
    {
        private readonly SealDbContext _context;

        public MentorAssignmentRepository(SealDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MentorAssignment>> GetTeamsByMentorIdAsync(int mentorId)
        {
            return await _context.MentorAssignments
                .Include(ma => ma.Team)
                    .ThenInclude(t => t.TeamLeader)
                .Where(ma => ma.MentorId == mentorId)
                .ToListAsync();
        }
    }
}
