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

        public async Task<TeamDto> CreateTeamAsync(CreateTeamDto dto, int userId)
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
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (verification == null || !string.Equals(verification.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Student has not been approved for verification. Cannot create team.");

            // 4 Map & create team
            var entity = _mapper.Map<Team>(dto);
            entity.HackathonId = null;
            entity.TeamLeaderId = userId;

            await _uow.Teams.AddAsync(entity);
            await _uow.SaveAsync();

            // 5️ Add leader in TeamMembers after create team
            var leaderMember = new TeamMember
            {
                TeamId = entity.TeamId,
                UserId = userId,
                RoleInTeam = "Leader"
            };
            await _uow.TeamMembers.AddAsync(leaderMember);
            await _uow.SaveAsync();

            // 6. Reload with includes
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

            await EnsureTeamEditableAsync(id);


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

            await EnsureTeamEditableAsync(id);

            _uow.Teams.Remove(team);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<IEnumerable<TeamDto>> GetTeamsByChapterIdAsync(int chapterId)
        {
            var teams = await _uow.Teams.GetAllIncludingAsync(
                t => t.ChapterId == chapterId,
                t => t.TeamLeader,
                t => t.Chapter,
                t => t.Hackathon
            );
            return _mapper.Map<IEnumerable<TeamDto>>(teams);
        }

        public async Task<IEnumerable<TeamDto>> GetTeamsByPhaseIdAsync(int phaseId)
        {
            var phase = await _uow.HackathonPhases.GetByIdAsync(phaseId);
            if (phase == null)
                throw new Exception("Phase not found.");

            // Lấy tất cả phase cùng hackathon
            var phases = await _uow.HackathonPhases.GetAllAsync(p => p.HackathonId == phase.HackathonId);

            // Tìm phase cuối cùng (EndDate lớn nhất)
            var lastPhase = phases
                .Where(p => p.EndDate.HasValue)
                .OrderByDescending(p => p.EndDate.Value)
                .FirstOrDefault();

            bool isLastPhase = lastPhase != null && lastPhase.PhaseId == phaseId;

            IEnumerable<Team> teams;

            if (isLastPhase)
            {
                // Phase cuối → lấy từ FinalQualification của phase trước (không phải phase cuối)
                var previousPhase = phases
                    .Where(p => p.PhaseId != phaseId)
                    .OrderByDescending(p => p.EndDate)
                    .FirstOrDefault();

                if (previousPhase == null)
                    throw new Exception("No previous phase found for final qualification.");

                var finalTeams = await _uow.FinalQualifications.GetAllIncludingAsync(
                    fq => fq.PhaseId == previousPhase.PhaseId,
                    fq => fq.Team,
                    fq => fq.Team.TeamLeader,
                    fq => fq.Team.Chapter,
                    fq => fq.Team.Hackathon
                );

                teams = finalTeams.Select(fq => fq.Team).Distinct();
            }
            else
            {
                // Phase bình thường → lấy team theo hackathon
                teams = await _uow.Teams.GetAllIncludingAsync(
                    t => t.HackathonId == phase.HackathonId,
                    t => t.TeamLeader,
                    t => t.Chapter,
                    t => t.Hackathon
                );
            }

            return _mapper.Map<IEnumerable<TeamDto>>(teams);
        }

        public async Task<IEnumerable<TeamDto>> GetUserTeamsAsync(int userId)
        {
            // Lấy team mà user là leader
            var leaderTeams = await _uow.Teams.GetAllIncludingAsync(
                t => t.TeamLeaderId == userId,
                t => t.TeamLeader,
                t => t.Chapter,
                t => t.Hackathon
            );

            // Lấy team mà user là member
            var memberTeams = await _uow.Teams.GetAllIncludingAsync(
                t => t.TeamMembers.Any(tm => tm.UserId == userId),
                t => t.TeamLeader,
                t => t.Chapter,
                t => t.Hackathon
            );

            // Combine và remove duplicates
            var allTeams = leaderTeams.Union(memberTeams).Distinct();

            return _mapper.Map<IEnumerable<TeamDto>>(allTeams);
        }

        private async Task EnsureTeamEditableAsync(int teamId)
        {
            var registration = await _uow.HackathonRegistrations
                .FirstOrDefaultAsync(r => r.TeamId == teamId);

            if (registration != null && registration.Status != "Cancelled")
                throw new InvalidOperationException(
                    "This team cannot be modified because it has registered for a hackathon."
                );
        }


    }
}
