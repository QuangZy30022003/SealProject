using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Repositories.Interface;
using Repositories.Models;
using Repositories.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Repositories.UnitOfWork
{
    public class UOW : IUOW
    {
        private readonly SealDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IRepository<User> Users { get; }
        public IRepository<Role> Roles { get; }
        public IRepository<PartnerProfile> PartnerProfiles { get; }
        public IRepository<StudentVerification> StudentVerifications { get; }


        public IRepository<Chapter> Chapters { get; }
        public IRepository<Team> Teams { get; }
        public IRepository<HackathonRegistration> HackathonRegistrations { get; }
        public IRepository<TeamMember> TeamMembers { get; }
        public IRepository<TeamInvitation> TeamInvitations { get; }
        public IRepository<TeamJoinRequest> TeamJoinRequests { get; }
        public IRepository<MentorAssignment> MentorAssignments { get; }

        public IRepository<ChatGroup> ChatGroups { get; }
        public IRepository<ChatMessage> ChatMessages { get; }
        public IRepository<ChatMessageRead> ChatMessageReads { get; }


        public IRepository<JudgeAssignment> JudgeAssignments { get; }
        public IRepository<Submission> Submissions { get; }
        public IRepository<Score> Scores { get; }
        public IRepository<PenaltiesBonuse> PenaltiesBonuses { get; }
        public IRepository<Appeal> Appeals { get; }
        public IRepository<Ranking> Rankings { get; }
        public IRepository<ScoreHistory> ScoreHistorys { get; }


        public IRepository<Criterion> Criteria { get; }
        public IRepository<CriterionDetail> CriterionDetail { get; }


        public IRepository<Prize> Prizes { get; }
        public IRepository<PrizeAllocation> PrizeAllocations { get; }


        public IRepository<AuditLog> AuditLogs { get; }
        public IRepository<Notification> Notifications { get; }


        public IRepository<Season> Seasons { get; }
        public IRepository<Hackathon> Hackathons { get; }
        public IRepository<HackathonPhase> HackathonPhases { get; }
        public IRepository<Challenge> Challenges { get; }


        public IRepository<Track> Tracks { get; }
        public IRepository<TeamTrackSelection> TeamTrackSelections { get; }
        public IRepository<Group> Groups { get; }
        public IRepository<GroupTeam> GroupsTeams { get; }


        public IRepository<FinalQualification> FinalQualifications { get; }
        public IRepository<ScheduleEvent> ScheduleEvents { get; }


        public IChallengeRepository ChallengeRepository { get; }
        public IHackathonPhaseRepository HackathonPhaseRepository { get; }
        public IMentorAssignmentRepository MentorAssignmentRepository { get; }
        public IScoreRepository ScoreRepository { get; }
        public ICriterionDetailRepository CriterionDetailRepository { get; }
        public IPrizeRepository PrizeRepository { get; }
        public IRepository<MentorVerification> MentorVerifications { get; }


        public UOW(SealDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            Users = new GenericRepository<User>(_context);
            StudentVerifications = new GenericRepository<StudentVerification>(_context);
            Roles = new GenericRepository<Role>(_context);
            MentorVerifications = new GenericRepository<MentorVerification>(_context);

            Chapters = new GenericRepository<Chapter>(_context);
            Teams = new GenericRepository<Team>(_context);
            TeamMembers = new GenericRepository<TeamMember>(_context);
            TeamInvitations = new GenericRepository<TeamInvitation>(_context);
            TeamJoinRequests = new GenericRepository<TeamJoinRequest>(_context);
            MentorAssignments = new GenericRepository<MentorAssignment>(_context);

            ChatGroups = new GenericRepository<ChatGroup>(_context);
            ChatMessages = new GenericRepository<ChatMessage>(_context);
            ChatMessageReads = new GenericRepository<ChatMessageRead>(_context);

            Prizes = new GenericRepository<Prize>(_context);
            PrizeAllocations = new GenericRepository<PrizeAllocation>(_context);

            AuditLogs = new GenericRepository<AuditLog>(_context);
            Notifications = new GenericRepository<Notification>(_context);

            Seasons = new GenericRepository<Season>(_context);
            Hackathons = new GenericRepository<Hackathon>(_context);
            HackathonPhases = new GenericRepository<HackathonPhase>(_context);
            Challenges = new GenericRepository<Challenge>(_context);

            Submissions = new GenericRepository<Submission>(_context);
            Scores = new GenericRepository<Score>(_context);
            PenaltiesBonuses = new GenericRepository<PenaltiesBonuse>(_context);
            Appeals = new GenericRepository<Appeal>(_context);
            Rankings = new GenericRepository<Ranking>(_context);
            ScoreHistorys = new GenericRepository<ScoreHistory>(_context);

            Criteria = new GenericRepository<Criterion>(_context);
            CriterionDetail = new GenericRepository<CriterionDetail>(_context);

            Tracks = new GenericRepository<Track>(_context);
            TeamTrackSelections = new GenericRepository<TeamTrackSelection>(_context);
            Groups = new GenericRepository<Group>(_context);
            GroupsTeams = new GenericRepository<GroupTeam>(_context);

            FinalQualifications = new GenericRepository<FinalQualification>(_context);
            ScheduleEvents = new GenericRepository<ScheduleEvent>(_context);


            HackathonRegistrations = new GenericRepository<HackathonRegistration>(_context);

            ChallengeRepository = new ChallengeRepository(_context);

            HackathonPhaseRepository = new HackathonPhaseRepository(_context);

            MentorAssignmentRepository = new MentorAssignmentRepository(_context);

            ScoreRepository = new ScoreRepository(_context);

            CriterionDetailRepository = new CriterionDetailRepository(_context);

            PrizeRepository = new PrizeRepository(_context);

            JudgeAssignments = new GenericRepository<JudgeAssignment>(_context);
            Submissions = new GenericRepository<Submission>(_context);
            PartnerProfiles = new GenericRepository<PartnerProfile>(_context);
        }

        //public async Task<int> SaveAsync()
        //{
        //    return await _context.SaveChangesAsync();
        //}
        public async Task<int> SaveAsync(int? explicitUserId = null)
        {
            int? userId = explicitUserId ?? GetCurrentUserId();

            var entries = _context.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                if (userId == null)
                    continue;

                object filteredData(object obj)
                {
                    if (obj == null) return null;

                    var type = obj.GetType();
                    var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p =>
                            p.PropertyType.IsPrimitive ||
                            p.PropertyType == typeof(string) ||
                            p.PropertyType == typeof(DateTime) ||
                            p.PropertyType == typeof(decimal) ||
                            p.PropertyType == typeof(Guid) ||
                            Nullable.GetUnderlyingType(p.PropertyType)?.IsPrimitive == true
                        );

                    var dict = new Dictionary<string, object>();
                    foreach (var prop in props)
                    {
                        var value = prop.GetValue(obj);
                        if (value != null) // chỉ lưu giá trị khác null
                            dict[prop.Name] = value;
                    }
                    return dict;
                }

                var audit = new AuditLog
                {
                    UserId = userId.Value,
                    Action = entry.State.ToString().ToUpper(),
                    CreatedAt = DateTime.UtcNow
                };

                if (entry.State == EntityState.Added)
                {
                    audit.Details = JsonSerializer.Serialize(
                        filteredData(entry.CurrentValues.ToObject()),
                        new JsonSerializerOptions { WriteIndented = true }
                    );
                }
                else if (entry.State == EntityState.Modified)
                {
                    var oldValues = filteredData(entry.OriginalValues.ToObject());
                    var newValues = filteredData(entry.CurrentValues.ToObject());

                    audit.Details = JsonSerializer.Serialize(
                        new { Before = oldValues, After = newValues },
                        new JsonSerializerOptions { WriteIndented = true }
                    );
                }
                else if (entry.State == EntityState.Deleted)
                {
                    audit.Details = JsonSerializer.Serialize(
                        filteredData(entry.OriginalValues.ToObject()),
                        new JsonSerializerOptions { WriteIndented = true }
                    );
                }

                await AuditLogs.AddAsync(audit);
            }

            return await _context.SaveChangesAsync();
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out var id))
                return id;

            return null; // trả null nếu claim không tồn tại
        }

        public void Dispose()
        {
            _context.Dispose();
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}
