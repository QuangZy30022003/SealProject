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

        }
    }
}
