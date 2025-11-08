using Repositories.Interface;
using Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Models;
using Microsoft.EntityFrameworkCore.Storage;
namespace Repositories.UnitOfWork
{
    public interface IUOW : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<Role> Roles { get; }
        IRepository<PartnerProfile> PartnerProfiles { get; }
        IRepository<StudentVerification> StudentVerifications { get; }


        IRepository<Chapter> Chapters { get; }
        IRepository<Team> Teams { get; }
        IRepository<HackathonRegistration> HackathonRegistrations { get; }
        IRepository<TeamMember> TeamMembers { get; }
        IRepository<TeamInvitation> TeamInvitations { get; }
        IRepository<TeamJoinRequest> TeamJoinRequests { get; }
        IRepository<MentorAssignment> MentorAssignments { get; }


        IRepository<Prize> Prizes { get; }
        IRepository<PrizeAllocation> PrizeAllocations { get; }


        IRepository<AuditLog> AuditLogs { get; }
        IRepository<Notification> Notifications { get; }


        IRepository<JudgeAssignment> JudgeAssignments { get; }
        IRepository<Submission> Submissions { get; }
        IRepository<Score> Scores { get; }
        IRepository<PenaltiesBonuse> PenaltiesBonuses { get; }
        IRepository<Appeal> Appeals { get; }
        IRepository<Ranking> Rankings { get; }


        IRepository<Season> Seasons { get; }
        IRepository<Hackathon> Hackathons { get; }
        IRepository<HackathonPhase> HackathonPhases { get; }
        IRepository<Challenge> Challenges { get; }


        IRepository<Criterion> Criteria { get; }
        IRepository<CriterionDetail> CriterionDetail { get; }


        IRepository<Track> Tracks { get; }
        IRepository<TeamTrackSelection> TeamTrackSelections { get; }
        IRepository<Group> Groups { get; }
        IRepository<GroupTeam> GroupsTeams { get; }


        IRepository<FinalQualification> FinalQualifications { get; }
        IRepository<ScheduleEvent> ScheduleEvents { get; }


        IChallengeRepository ChallengeRepository { get; }
        IHackathonPhaseRepository HackathonPhaseRepository { get; }
        IMentorAssignmentRepository MentorAssignmentRepository { get; }
        IScoreRepository ScoreRepository { get; }
        IPrizeRepository PrizeRepository { get; }
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<int> SaveAsync();
    }
}
