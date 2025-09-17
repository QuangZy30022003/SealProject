﻿using AutoMapper;
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

        public async Task<IEnumerable<TeamDto>> GetAllAsync()
        {
            var teams = await _uow.Teams.GetAllAsync(includeProperties: "Chapter");
            return _mapper.Map<IEnumerable<TeamDto>>(teams);
        }

        public async Task<TeamDto?> UpdateAsync(int id, UpdateTeamDto dto)
        {
            var team = await _uow.Teams.GetByIdAsync(id);
            if (team == null) return null;

            // check nếu đổi tên thì tên có trùng không
            if (!string.Equals(team.TeamName, dto.TeamName, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _uow.TeamsRepository.ExistsByNameAsync(dto.TeamName, dto.ChapterId);
                if (exists)
                    throw new Exception("Team name already exists in this chapter");
            }

            team.TeamName = dto.TeamName;
            team.ChapterId = dto.ChapterId;

            _uow.Teams.Update(team);
            await _uow.SaveAsync();

            return _mapper.Map<TeamDto>(team);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var team = await _uow.Teams.GetByIdAsync(id);
            if (team == null) return false;

            _uow.Teams.Remove(team);
            await _uow.SaveAsync();
            return true;
        }
    }
}
