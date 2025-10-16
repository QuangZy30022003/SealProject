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
            service.AddScoped<ITeamChallengeService, TeamChallengeService>();
            service.AddScoped<ITeamMemberService, TeamMemberService>();
            service.AddScoped<IStudentVerificationService, StudentVerificationService>();
            service.AddScoped<IPhaseChallengeService, PhaseChallengeService>();
            service.AddScoped<IPenaltyService, PenaltyService>();
            service.AddScoped<IAppealService, AppealService>();
            service.AddScoped<ISubmissionService, SubmissionService>();
            return service;

        }
    }
}
