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
    public class TeamInvitationRepository : GenericRepository<TeamInvitation>, ITeamInvitationRepository
    {
        private readonly SealDbContext _context;
        public TeamInvitationRepository(SealDbContext context) : base(context) 
        {
            _context = context;
        }

        public async Task<TeamInvitation?> GetByCodeAsync(Guid code)
        {
            return await _context.TeamInvitations
                .FirstOrDefaultAsync(x => x.InvitationCode == code);
        }
    }
}
