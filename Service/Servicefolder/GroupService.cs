using AutoMapper;
using Common.DTOs.GroupDto;
using Microsoft.EntityFrameworkCore;
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
    public class GroupService : IGroupService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public GroupService(IUOW uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<List<GroupDto>> CreateGroupsByTrackAsync(CreateGroupsRequestDto dto)
        {
            if (dto.TeamsPerGroup <= 0)
                throw new ArgumentException("TeamsPerGroup must be greater than 0");

            // Lấy tất cả team track selection
            var allTeamSelections = (await _uow.TeamTrackSelections.GetAllAsync()).ToList();
            var trackIds = allTeamSelections.Select(t => t.TrackId).Distinct();

            var createdGroups = new List<Group>();

            foreach (var trackId in trackIds)
            {
                var teamIds = allTeamSelections
                    .Where(t => t.TrackId == trackId)
                    .OrderBy(t => t.SelectedAt)
                    .Select(t => t.TeamId)
                    .ToList();

                if (!teamIds.Any()) continue;

                int groupCount = (int)Math.Ceiling(teamIds.Count / (double)dto.TeamsPerGroup);
                var groupNameChar = 'A';

                for (int i = 0; i < groupCount; i++)
                {
                    var group = new Group
                    {
                        TrackId = trackId,
                        GroupName = groupNameChar.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.Groups.AddAsync(group);
                    await _uow.SaveAsync(); // cần lưu để có GroupId
                    createdGroups.Add(group);

                    var teamsInGroup = teamIds
                        .Skip(i * dto.TeamsPerGroup)
                        .Take(dto.TeamsPerGroup)
                        .ToList();

                    foreach (var teamId in teamsInGroup)
                    {
                        var groupTeam = new GroupTeam
                        {
                            GroupId = group.GroupId,
                            TeamId = teamId,
                            JoinedAt = DateTime.UtcNow
                        };
                        await _uow.GroupsTeams.AddAsync(groupTeam);
                    }

                    groupNameChar++;
                }
            }

            await _uow.SaveAsync();

            // Map sang DTO và thêm danh sách teamId
            var result = _mapper.Map<List<GroupDto>>(createdGroups);
            foreach (var g in result)
            {
                var teams = (await _uow.GroupsTeams.GetAllAsync(gt => gt.GroupId == g.GroupId)).Select(t => t.TeamId).ToList();
                g.TeamIds = teams;
            }

            return result;
        }

        public async Task<List<GroupDto>> GetGroupsByHackathonAsync(int hackathonId)
        {
            // Lấy tất cả phase thuộc hackathon
            var phases = await _uow.HackathonPhases.GetAllAsync(p => p.HackathonId == hackathonId);
            if (!phases.Any())
                return new List<GroupDto>();

            var phaseIds = phases.Select(x => x.PhaseId).ToList();

            // Lấy tất cả track thuộc các phase
            var tracks = await _uow.Tracks.GetAllAsync(t => phaseIds.Contains(t.PhaseId));
            if (!tracks.Any())
                return new List<GroupDto>();

            var trackIds = tracks.Select(t => t.TrackId).ToList();

            // Lấy group theo các track
            var groups = await _uow.Groups.GetAllAsync(g => trackIds.Contains(g.TrackId));
            if (!groups.Any())
                return new List<GroupDto>();

            var result = _mapper.Map<List<GroupDto>>(groups);

            // Lấy thêm teamIds
            foreach (var g in result)
            {
                var teamIds = (await _uow.GroupsTeams.GetAllAsync(gt => gt.GroupId == g.GroupId))
                    .Select(gt => gt.TeamId)
                    .ToList();

                g.TeamIds = teamIds;
            }

            return result;
        }
        public async Task<List<GroupTeamDto>> GetGroupTeamsByGroupIdAsync(int groupId)
        {
            // Lấy Group để kiểm tra tồn tại
            var group = await _uow.Groups.GetByIdAsync(groupId);
            if (group == null)
                throw new ArgumentException("GroupId không tồn tại");

            // Lấy danh sách GroupTeam + Include Team
            var groupTeams = await _uow.GroupsTeams.GetAllIncludingAsync(
                g => g.GroupId == groupId,
                g => g.Team
            );

            return _mapper.Map<List<GroupTeamDto>>(groupTeams);
        }

    }
}