using AutoMapper;
using Common.DTOs.ChapterDto;
using Common.DTOs.TeamDto;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class TeamService: ITeamService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public TeamService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<TeamDto> CreateTeamAsync(CreateTeamDto dto)
        {
            var chapter = await _uow.Chapters.GetByIdAsync(dto.ChapterId);
            if (chapter == null)
            {
                throw new Exception("Chapter does not exist. Please create a chapter first.");
            }
            // check trùng tên trong cùng Chapter
            var exists = await _uow.TeamsRepository.ExistsByNameAsync(dto.TeamName, dto.ChapterId);
            if (exists)
            {
                throw new Exception("Team name already exists in this chapter");
            }

            var entity = _mapper.Map<Team>(dto);
            await _uow.Teams.AddAsync(entity);
            await _uow.SaveAsync();

            return _mapper.Map<TeamDto>(entity);
        }
        public async Task<TeamDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.Teams.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<TeamDto>(entity);
        }
    }
}
