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
            CreateMap<Season, SeasonResponse>();
            CreateMap<SeasonRequest, Season>()
      .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.SeasonCode));
            CreateMap<SeasonUpdateDto, Season>();
                

        }
    }
}
