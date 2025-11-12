using Common.DTOs.ChapterDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Models;
using AutoMapper;
using Common.DTOs.TeamDto;
using Common.DTOs.SeasonDto;
using Common.DTOs.ChallengeDto;
using Common.DTOs.TeamInvitationDto;
using Common.DTOs.HackathonPhaseDto;
using Common.DTOs.HackathonDto;
using Common.DTOs.AssignedTeamDto;
using Common.DTOs.TeamMemberDto;
using Common.DTOs.StudentVerification;
using Common.DTOs.AppealDto;
using Common.DTOs.PenaltyBonusDto;
using Common.DTOs.Submission;
using Common.DTOs.CriterionDTO;
using Common.DTOs.ScoreDto;
using Common.DTOs.PrizeDto;
using Common.DTOs.TeamJoinRequestDto;
using Common.DTOs.AuthDto;
using Common.DTOs.RegisterHackathonDto;
using Common.DTOs.TrackDto;
using Common.DTOs.TeamTrackDto;
using Common.DTOs.JudgeAssignmentDto;
using Common.DTOs.GroupDto;



namespace Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Chapter
            CreateMap<CreateChapterDto, Chapter>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.ChapterLeaderId, opt => opt.Ignore())
                .ForMember(dest => dest.Teams, opt => opt.Ignore())
                .ForMember(dest => dest.ChapterLeader, opt => opt.Ignore());
            CreateMap<UpdateChapterDto, Chapter>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Teams, opt => opt.Ignore())
                .ForMember(dest => dest.ChapterLeader, opt => opt.Ignore());
            CreateMap<Chapter, ChapterDto>()
            .ForMember(dest => dest.ChapterLeaderName,
                opt => opt.MapFrom(src => src.ChapterLeader != null
                    ? src.ChapterLeader.FullName
                    : "(No leader)"));

            // team
            CreateMap<CreateTeamDto, Team>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdateTeamDto, Team>();

            CreateMap<Team, TeamDto>()
                // Map basic info
                .ForMember(dest => dest.TeamLeaderName,
                    opt => opt.MapFrom(src => src.TeamLeader != null ? src.TeamLeader.FullName : "(No leader)"))

                // Map Chapter name (if Chapter navigation is loaded)
                .ForMember(dest => dest.ChapterName,
                    opt => opt.MapFrom(src => src.Chapter != null ? src.Chapter.ChapterName : "(No chapter)"))

                // Map Hackathon name (if Hackathon navigation is loaded)
                .ForMember(dest => dest.HackathonName,
                    opt => opt.MapFrom(src => src.Hackathon != null ? src.Hackathon.Name : "(No hackathon)"));
            // Seasaon
            CreateMap<Season, SeasonResponse>();
            CreateMap<SeasonRequest, Season>()
      .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.SeasonCode));
            CreateMap<SeasonUpdateDto, Season>();
            CreateMap<Season, SeasonResponse>()
    .ForMember(dest => dest.SeasonCode, opt => opt.MapFrom(src => src.Code)); 

            // Challenge

            CreateMap<Challenge, ChallengeDto>()
               .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName));

            CreateMap<ChallengeCreateUnifiedDto, Challenge>()
                .ForMember(dest => dest.FilePath, opt => opt.Ignore()) // handle file manually
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Pending"))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<ChallengePartnerUpdateDto, Challenge>();
            CreateMap<Challenge, ChallengeViewDto>();

            //  TeamInvitation 
            CreateMap<TeamInvitation, InvitationStatusDto>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.TeamName,
                    opt => opt.MapFrom(src => src.Team.TeamName))
                .ForMember(dest => dest.IsExpired,
                    opt => opt.MapFrom(src => src.ExpiresAt < DateTime.UtcNow));


            // HackathonPhaseProfile
            CreateMap<HackathonPhase, HackathonPhaseDto>().ReverseMap();
            CreateMap<HackathonPhaseCreateDto, HackathonPhase>();
            CreateMap<HackathonPhaseUpdateDto, HackathonPhase>();
            // Hackathon chi tiết
            CreateMap<Hackathon, HackathonDetailResponseDto>()
      .ForMember(dest => dest.Season, opt => opt.MapFrom(src => src.Season.Code))
      .ForMember(dest => dest.Phases, opt => opt.MapFrom(src => src.HackathonPhases))
      .ForMember(dest => dest.Prizes, opt => opt.MapFrom(src => src.Prizes));

            CreateMap<HackathonPhase, HackathonPhaseDtos>();
            CreateMap<Prize, PrizeDto>();

            //Hackathon

            CreateMap<Hackathon, HackathonResponseDto>()
                .ForMember(dest => dest.SeasonId, opt => opt.MapFrom(src => src.SeasonId))
                .ForMember(dest => dest.SeasonName, opt => opt.MapFrom(src => src.Season.Name)); CreateMap<HackathonCreateDto, Hackathon>();
            CreateMap<HackathonDto, Hackathon>();

            CreateMap<MentorAssignment, MentorAssignmentResponseDto>()
              .ForMember(dest => dest.MentorId, opt => opt.MapFrom(src => src.MentorId))
              .ForMember(dest => dest.TeamId, opt => opt.MapFrom(src => src.TeamId))
              .ForMember(dest => dest.HackathonId, opt => opt.MapFrom(src => src.HackathonId));

            //team member
            CreateMap<TeamMember, TeamMemberDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email));

            // StudentVerify
            CreateMap<StudentVerificationDto, StudentVerification>()
            .ForMember(dest => dest.StudentEmail, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.FrontCardImage, opt => opt.Ignore())
            .ForMember(dest => dest.BackCardImage, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Pending"));

            // PenaltiesBonuse
            CreateMap<PenaltiesBonuse, PenaltiesBonuseResponseDto>()
    .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Team != null ? src.Team.TeamName : null));
            //.ForMember(dest => dest.HackathonName, opt => opt.MapFrom(src => src.Hackathon != null ? src.Hackathon.Name : null));
            CreateMap<CreatePenaltiesBonuseDto, PenaltiesBonuse>();


            // Appeal
            CreateMap<Appeal, AppealResponseDto>()
               .ForMember(dest => dest.ReviewedByName, opt => opt.MapFrom(src => src.ReviewedBy != null ? src.ReviewedBy.FullName : null));
            CreateMap<CreateAppealDto, Appeal>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "Pending"));

            //Submission

            CreateMap<Submission, SubmissionResponseDto>()
                .ForMember(dest => dest.TrackId,
                           opt => opt.MapFrom(src => src.Team.TeamTrackSelections.FirstOrDefault().TrackId));
            CreateMap<SubmissionCreateDto, Submission>();
            CreateMap<SubmissionUpdateDto, Submission>();

            //Criterion
            CreateMap<Criterion, CriterionResponseDto>();
            CreateMap<CriterionCreateDto, Criterion>();
            CreateMap<CriterionUpdateDto, Criterion>();

            // Score
            // Map từ entity Score → DTO đọc ra
            CreateMap<Score, ScoreResponseDto>()
                 .ForMember(dest => dest.SubmissionName, opt => opt.MapFrom(src => src.Submission.Title))
                 .ForMember(dest => dest.CriteriaName, opt => opt.MapFrom(src => src.Criteria.Name))
                 .ForMember(dest => dest.ScoreValue, opt => opt.MapFrom(src => src.Score1));
            //payload

            CreateMap<HackathonCreatePayloadDto, Hackathon>();
            CreateMap<PhaseCreatePayloadDto, HackathonPhase>();
            CreateMap<PrizeCreatePayloadDto, Prize>();
            //   CreateMap<ChallengeCreateUnifiedPayloadDto, Challenge>();


            CreateMap<Prize, PrizeDTO>();
            CreateMap<CreatePrizeDTO, Prize>();
            CreateMap<UpdatePrizeDTO, Prize>();

            // TeamJoinRequest
            CreateMap<TeamJoinRequest, JoinRequestResponseDto>()
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Team != null ? src.Team.TeamName : null))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null));
            CreateMap<CreateJoinRequestDto, TeamJoinRequest>();

            //user
            CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null));
            // HackathonRegis
            CreateMap<RegisterHackathonRequest, HackathonRegistration>()
               .ForMember(dest => dest.RegistrationId, opt => opt.Ignore()) // DB tự generate
               .ForMember(dest => dest.RegisteredAt, opt => opt.Ignore())   // set trong service
               .ForMember(dest => dest.Status, opt => opt.Ignore());        // set trong service
            CreateMap<HackathonRegistration, HackathonRegistrationDto>()
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Team.TeamName))
                .ForMember(dest => dest.HackathonName, opt => opt.MapFrom(src => src.Hackathon.Name));

            //Track
            CreateMap<Track, TrackRespone>();
            CreateMap<CreateTrackDto, Track>();
            CreateMap<UpdateTrackDto, Track>();

            //TeamTrackSelection
            CreateMap<TeamTrackSelection, TeamSelectTrackResponse>();
            CreateMap<TeamSelectTrackRequest, TeamTrackSelection>();
            //jugdeAssign
            CreateMap<JudgeAssignment, JudgeAssignmentResponseDto>()
    .ForMember(dest => dest.JudgeName,
        opt => opt.MapFrom(src => src.Judge != null ? src.Judge.FullName : null))
    .ForMember(dest => dest.TrackName,
        opt => opt.MapFrom(src => src.Track != null ? src.Track.Name : null))
    .ForMember(dest => dest.HackathonName,
        opt => opt.MapFrom(src => src.Hackathon != null ? src.Hackathon.Name : null))
    .ForMember(dest => dest.Status,
        opt => opt.MapFrom(src => src.Status))
    .ForMember(dest => dest.AssignedAt,
        opt => opt.MapFrom(src => src.AssignedAt))
    .ForMember(dest => dest.AssignmentId,
        opt => opt.MapFrom(src => src.AssignmentId))
    .ForMember(dest => dest.JudgeId,
        opt => opt.MapFrom(src => src.JudgeId))
    .ForMember(dest => dest.HackathonId,
        opt => opt.MapFrom(src => src.HackathonId))
    .ForMember(dest => dest.TrackId,
        opt => opt.MapFrom(src => src.TrackId));

            //group
            CreateMap<Group, GroupDto>();
        }
    }
}
