using Microsoft.Extensions.DependencyInjection;
using Service.Interface;
using Service.Servicefolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddService(this IServiceCollection service)
        {
           service.AddScoped<IRoleService, RoleService>();
            service.AddScoped<IAuthService, AuthService>();
            service.AddScoped<IEmailService, EmailService>();
            service.AddScoped<ITeamService, TeamService>();
            service.AddScoped<IChapterService, ChapterService>();
            service.AddScoped<ISeasonService, SeasonService>();
            service.AddScoped<ITeamInvitationService, TeamInvitationService>();
            service.AddScoped<IChallengeService, ChallengeService>();
            service.AddScoped<IHackathonService, HackathonService>();
            service.AddScoped<IHackathonPhaseService, HackathonPhaseService>();
            service.AddScoped<IMentorAssignmentService, MentorAssignmentService>();
            service.AddScoped<ITeamMemberService, TeamMemberService>();
            service.AddScoped<IStudentVerificationService, StudentVerificationService>();
            service.AddScoped<IPenaltyService, PenaltyService>();
            service.AddScoped<IAppealService, AppealService>();
            service.AddScoped<ISubmissionService, SubmissionService>();
            service.AddScoped<ICriterionService, CriterionService>();
            service.AddScoped<IScoreService, ScoreService>();

            service.AddScoped<IChatService, ChatService>();
            service.AddScoped<IPrizeService, PrizeService>();

            service.AddScoped<ITeamJoinRequestService, TeamJoinRequestService>();
            service.AddScoped<IFileUploadService, FileUploadService>();
            service.AddScoped<IHackathonRegistrationService, HackathonRegistrationService>();   

            service.AddScoped<ITrackService, TrackService>();
            service.AddScoped<ITeamTrackService, TeamTrackService>();
            service.AddScoped<IJudgeAssignmentService, JudgeAssignmentService>();
            service.AddScoped<IGroupService, GroupService>();
            service.AddScoped<IQualificationService, QualificationService>();
            return service;

        }
    }
}
