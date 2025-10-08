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
using Common.DTOs.TeamChallengeDto;
using Common.DTOs.TeamMemberDto;
using Common.DTOs.StudentVerification;



namespace Common.Mappings
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            // Chapter
            CreateMap<CreateChapterDto, Chapter>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
            CreateMap<UpdateChapterDto, Chapter>();
            CreateMap<Chapter, ChapterDto>();

            CreateMap<CreateTeamDto, Team>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
            CreateMap<UpdateTeamDto, Team>();
            CreateMap<Team, TeamDto>();
            // Seasaon
            CreateMap<Season, SeasonResponse>();
            CreateMap<SeasonRequest, Season>()
      .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.SeasonCode));
            CreateMap<SeasonUpdateDto, Season>();
            // Challenge

            CreateMap<Challenge, ChallengeDto>()
               .ForMember(dest => dest.SeasonName, opt => opt.MapFrom(src => src.Season.Name))
               .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName));

            CreateMap<ChallengeCreateUnifiedDto, Challenge>()
                .ForMember(dest => dest.FilePath, opt => opt.Ignore()) // handle file manually
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Pending"))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<ChallengeUpdateDto, Challenge>();


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

            //Hackathon

            CreateMap<Hackathon, HackathonResponseDto>();
              //   .ForMember(dest => dest.SeasonName, opt => opt.MapFrom(src => src.Season));
            CreateMap<HackathonCreateDto, Hackathon>();
            CreateMap<HackathonDto, Hackathon>();

            CreateMap<MentorAssignment, MentorAssignmentResponseDto>()
              .ForMember(dest => dest.MentorId, opt => opt.MapFrom(src => src.MentorId))
              .ForMember(dest => dest.TeamId, opt => opt.MapFrom(src => src.TeamId))
              .ForMember(dest => dest.ChapterId, opt => opt.MapFrom(src => src.ChapterId));
            // TEAM CHALLENGE 
            CreateMap<TeamChallenge, TeamChallengeResponseDto>()
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Team.TeamName))
                .ForMember(dest => dest.HackathonName, opt => opt.MapFrom(src => src.Hackathon.Name))
                .ReverseMap();

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
        }
    }
}
