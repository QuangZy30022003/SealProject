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
    public class ChapterService : IChapterService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public ChapterService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<TeamDto> CreateTeamAsync(CreateTeamDto dto)
        {
            // 1. Check chapter existence
            if (!dto.ChapterId.HasValue)
                throw new InvalidOperationException("ChapterId is required");

            var chapterExists = await _uow.Chapters.ExistsAsync(c => c.ChapterId == dto.ChapterId.Value);
            if (!chapterExists)
                throw new InvalidOperationException("Chapter does not exist. Please create a chapter first.");

            // 2. Check duplicate name within same chapter
            var exists = await _uow.Teams.ExistsAsync(t =>
               t.TeamName == dto.TeamName &&
               t.ChapterId == dto.ChapterId);
            if (exists)
                throw new InvalidOperationException("Team name already exists in this chapter.");

            // 3 Check student verification
            var verification = await _uow.StudentVerifications
                .FirstOrDefaultAsync(v => v.UserId == dto.TeamLeaderId);

            if (verification == null || !string.Equals(verification.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Student has not been approved for verification. Cannot create team.");

            // 4 Map & create team
            var entity = _mapper.Map<Team>(dto);
            entity.HackathonId = null;

            await _uow.Teams.AddAsync(entity);
            await _uow.SaveAsync();

            // 5. Reload with includes
            var created = await _uow.Teams.GetByIdIncludingAsync(
                t => t.TeamId == entity.TeamId,
                t => t.TeamLeader,
                t => t.Chapter,
                t => t.Hackathon
            );

            return _mapper.Map<TeamDto>(created);
        }

        public async Task<TeamDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.Teams.GetByIdIncludingAsync(
                t => t.TeamId == id,
                t => t.TeamLeader,
                t => t.Chapter,
                t => t.Hackathon
            );
            return entity == null ? null : _mapper.Map<TeamDto>(entity);
        }

        public async Task<IEnumerable<TeamDto>> GetAllAsync()
        {
            var teams = await _uow.Teams.GetAllIncludingAsync(
                null,
                t => t.TeamLeader,
                t => t.Chapter,
                t => t.Hackathon
            );
            return _mapper.Map<IEnumerable<TeamDto>>(teams);
        }

        public async Task<TeamDto?> UpdateAsync(int id, UpdateTeamDto dto)
        {
            var team = await _uow.Teams.GetByIdAsync(id);
            if (team == null) return null;

            // Normalize 0 → null
            if (dto.ChapterId == 0) dto.ChapterId = null;

            // Change chapter if provided
            if (dto.ChapterId.HasValue && dto.ChapterId.Value != team.ChapterId)
            {
                var chapterExists = await _uow.Chapters.ExistsAsync(c => c.ChapterId == dto.ChapterId.Value);
                if (!chapterExists)
                    throw new InvalidOperationException("Chapter does not exist.");
                team.ChapterId = dto.ChapterId.Value;
            }

            // Change name if provided
            if (!string.IsNullOrWhiteSpace(dto.TeamName) &&
                !string.Equals(team.TeamName, dto.TeamName, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _uow.Teams.ExistsAsync(t =>
                    t.TeamName == dto.TeamName &&
                    t.ChapterId == team.ChapterId &&
                    t.TeamId != id);
                if (exists)
                    throw new InvalidOperationException("Team name already exists in this chapter.");

                team.TeamName = dto.TeamName;
            }

            _uow.Teams.Update(team);
            await _uow.SaveAsync();

            // Reload with include for return DTO
            var updated = await _uow.Teams.GetByIdIncludingAsync(
                t => t.TeamId == id,
                t => t.TeamLeader,
                t => t.Chapter,
                t => t.Hackathon
            );

            return _mapper.Map<TeamDto>(updated);
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var team = await _uow.Teams.GetByIdAsync(id);
            if (team == null) return false;

            if (team.TeamLeaderId != userId)
                throw new UnauthorizedAccessException("You are not the leader of this team.");

            _uow.Teams.Remove(team);
            await _uow.SaveAsync();
            return true;
        }
    }
}
