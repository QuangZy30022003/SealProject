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
        IRepository<Team> Teams { get; }
        IRepository<Chapter> Chapters { get; }
        IRepository<Submission> Submissions { get; }
        IRepository<MentorAssignment> MentorAssignments { get; }
        IRepository<Prize> Prizes { get; }
        IRepository<PrizeAllocation> PrizeAllocations { get; }
        IRepository<Role> Roles { get; }
        IRepository<AuditLog> AuditLogs { get; }
        IRepository<Notification> Notifications { get; }
        IRepository<Score> Scores { get; }
        IRepository<PenaltiesBonuse> PenaltiesBonuses { get; }
        IRepository<Hackathon> Hackathons { get; }
        IRepository<HackathonPhase> HackathonPhases { get; }
        IRepository<Criterion> Criteria { get; }
        IRepository<TeamMember> TeamMembers { get; }
        IRepository<TeamInvitation> TeamInvitations { get; }
        IRepository<StudentVerification> StudentVerifications { get; }
        IRepository<Appeal> Appeals { get; }
        IRepository<TeamChallenge> TeamChallenges { get; }
        IRepository<TeamJoinRequest> TeamJoinRequests { get; }
        IAuthRepository AuthRepository { get; }
        ITeamRepository TeamsRepository { get; }
        IChapterRepository ChaptersRepository { get; }
        ISeasonRepository SeasonRepository { get; }
        ITeamInvitationRepository TeamInvitationRepository { get; }

        IChallengeRepository ChallengeRepository { get; }

        IHackathonPhaseRepository HackathonPhaseRepository { get; }

        IMentorAssignmentRepository MentorAssignmentRepository { get; }

        IStudentVerificationRepository StudentVerificationRepository { get; }
        IRepository<PhaseChallenge> PhaseChallenges { get; }

        IRepository<CriterionDetail> CriterionDetail { get; }
        IScoreRepository ScoreRepository { get; }

        IPhaseChallengeRepository PhaseChallengeRepository { get; }

        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<int> SaveAsync();
    }
}
