using Repositories.Interface;
using Repositories.Models;
using Repositories.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.UnitOfWork
{
    public class UOW : IUOW
    {
        private readonly SealDbContext _context;

        public IRepository<User> Users { get; }
        public IRepository<Team> Teams { get; }
        public IRepository<Chapter> Chapters { get; }
        public IRepository<Submission> Submissions { get; }
        public IRepository<MentorAssignment> MentorAssignments { get; }
        public IRepository<Prize> Prizes { get; }
        public IRepository<PrizeAllocation> PrizeAllocations { get; }
        public IRepository<Role> Roles { get; }
        public IRepository<AuditLog> AuditLogs { get; }
        public IRepository<Notification> Notifications { get; }
        public IRepository<Score> Scores { get; }
        public IRepository<PenaltiesBonuse> PenaltiesBonuses { get; }
        public IRepository<Hackathon> Hackathons { get; }
        public IRepository<HackathonPhase> HackathonPhases { get; }
        public IRepository<Criterion> Criteria { get; }
        public IRepository<TeamMember> TeamMembers { get; }
        public IRepository<TeamInvitation> TeamInvitations { get; }
        public IAuthRepository AuthRepository { get; }
        public ITeamRepository TeamsRepository { get; }
        public IChapterRepository ChaptersRepository { get; }
        public ISeasonRepository SeasonRepository { get; }
        public ITeamInvitationRepository TeamInvitationRepository { get; }
        public IChallengeRepository ChallengeRepository { get; }
        public UOW(SealDbContext context)
        {
            _context = context;

            Users = new GenericRepository<User>(_context);
            Teams = new GenericRepository<Team>(_context);
            Chapters = new GenericRepository<Chapter>(_context);
            Submissions = new GenericRepository<Submission>(_context);
            MentorAssignments = new GenericRepository<MentorAssignment>(_context);
            Prizes = new GenericRepository<Prize>(_context);
            PrizeAllocations = new GenericRepository<PrizeAllocation>(_context);
            Roles = new GenericRepository<Role>(_context);
            AuditLogs = new GenericRepository<AuditLog>(_context);
            Notifications = new GenericRepository<Notification>(_context);
            Scores = new GenericRepository<Score>(_context);
            PenaltiesBonuses = new GenericRepository<PenaltiesBonuse>(_context);
            Hackathons = new GenericRepository<Hackathon>(_context);
            HackathonPhases = new GenericRepository<HackathonPhase>(_context);
            Criteria = new GenericRepository<Criterion>(_context);
            TeamMembers = new GenericRepository<TeamMember>(_context);
            TeamInvitations = new GenericRepository<TeamInvitation>(_context);
            AuthRepository = new AuthRepository(_context);
            TeamsRepository = new TeamRepository(_context);
            ChaptersRepository = new ChapterRepository(_context);
            SeasonRepository = new SeasonRepository(_context);
            TeamInvitationRepository = new TeamInvitationRepository(_context);
            ChallengeRepository = new ChallengeRepository(_context);
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
