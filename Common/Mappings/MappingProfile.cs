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
using Common.DTOs.AppealDto;
using Common.DTOs.PenaltyBonusDto;
using Common.DTOs.Submission;
using Common.DTOs.CriterionDTO;
using Common.DTOs.ScoreDto;
using Common.DTOs.PrizeDto;
using Common.DTOs.TeamJoinRequestDto;
using Common.DTOs.AuthDto;



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
            CreateMap<Season, SeasonResponse>()
    .ForMember(dest => dest.SeasonCode, opt => opt.MapFrom(src => src.Code)); // ✅ sửa lỗi null

            // Challenge

            CreateMap<Challenge, ChallengeDto>()
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
              .ForMember(dest => dest.ChapterId, opt => opt.MapFrom(src => src.ChapterId));
  
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

            CreateMap<SubmissionCreateDto, Submission>()
               .ForMember(dest => dest.SubmissionId, opt => opt.Ignore()) // ID tự sinh
               .ForMember(dest => dest.SubmittedBy, opt => opt.Ignore())  // gán thủ công trong service
               .ForMember(dest => dest.IsFinal, opt => opt.Ignore())      // gán mặc định
               .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore()); // gán DateTime.UtcNow

            CreateMap<Criterion, CriterionReadDTO>()
           .ForMember(dest => dest.Details,
               opt => opt.MapFrom(src => src.CriterionDetails));

            CreateMap<CriterionCreateDTO, Criterion>()
                .ForMember(dest => dest.CriterionDetails,
                    opt => opt.MapFrom(src => src.Details));

            CreateMap<CriterionUpdateDTO, Criterion>()
                .ForMember(dest => dest.CriterionDetails,
                    opt => opt.MapFrom(src => src.Details));

            CreateMap<CriterionDetail, CriterionDetailDTO>().ReverseMap();
            CreateMap<CriterionDetailCreateDTO, CriterionDetail>();
            // Score
            // Map từ entity Score → DTO đọc ra
            CreateMap<Score, ScoreReadDto>()
              .ForMember(dest => dest.CriteriaName, opt => opt.MapFrom(src => src.Criteria.Name))
              .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score1))
              .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Comment))
              .ForMember(dest => dest.JudgeName, opt => opt.MapFrom(src => src.Judge.FullName))
              .ForMember(dest => dest.ScoredAt, opt => opt.MapFrom(src => src.ScoredAt));
            // Map khi lưu dữ liệu (DTO → Entity)
            CreateMap<JudgeScoreDto, Score>()
              .ForMember(dest => dest.Score1, opt => opt.Ignore()) // tổng điểm sẽ tính trong service
              .ForMember(dest => dest.CriteriaId, opt => opt.MapFrom(src => src.CriteriaId))
              .ForMember(dest => dest.SubmissionId, opt => opt.MapFrom(src => src.SubmissionId))
              .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Comment))
              .ForMember(dest => dest.JudgeId, opt => opt.Ignore()) // gán trong service
              .ForMember(dest => dest.ScoredAt, opt => opt.Ignore())
              .ForMember(dest => dest.Criteria, opt => opt.Ignore())
              .ForMember(dest => dest.Judge, opt => opt.Ignore())
              .ForMember(dest => dest.Submission, opt => opt.Ignore());
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

        }
        }
    }
