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

            // 1️⃣ Lấy track trong phase
            var tracksInPhase = await _uow.Tracks.GetAllAsync(t => t.PhaseId == dto.PhaseId);
            var trackIds = tracksInPhase.Select(t => t.TrackId).ToList();

            if (!trackIds.Any())
                throw new Exception("No tracks found for this phase.");

            // 2️⃣ Xóa group cũ + groupTeam cũ
            var oldGroups = await _uow.Groups.GetAllAsync(g => trackIds.Contains(g.TrackId));

            if (oldGroups.Any())
            {
                var oldGroupIds = oldGroups.Select(g => g.GroupId).ToList();

                // Xóa GroupTeam trước
                var oldGroupTeams = await _uow.GroupsTeams.GetAllAsync(gt => oldGroupIds.Contains(gt.GroupId));
                foreach (var gt in oldGroupTeams)
                    _uow.GroupsTeams.Remove(gt);

                // Xóa Group
                foreach (var g in oldGroups)
                    _uow.Groups.Remove(g);

                await _uow.SaveAsync(); // Lưu để dọn sạch dữ liệu
            }

            // 3️⃣ Lấy team đã chọn track trong phase
            var allTeamSelections = await _uow.TeamTrackSelections
                .GetAllAsync(t => trackIds.Contains(t.TrackId));

            if (!allTeamSelections.Any())
                throw new Exception("No teams selected tracks in this phase.");

            var createdGroups = new List<Group>();
            var createdGroupTeams = new List<GroupTeam>();

            char groupNameChar = 'A';

            // 4️⃣ Chia nhóm theo Track
            foreach (var trackId in trackIds)
            {
                var teamIds = allTeamSelections
                    .Where(t => t.TrackId == trackId)
                    .OrderBy(t => t.SelectedAt)
                    .Select(t => t.TeamId)
                    .ToList();

                if (!teamIds.Any())
                    continue;

                int groupCount = (int)Math.Ceiling(teamIds.Count / (double)dto.TeamsPerGroup);

                for (int i = 0; i < groupCount; i++)
                {
                    var group = new Group
                    {
                        TrackId = trackId,
                        GroupName = groupNameChar.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.Groups.AddAsync(group);
                    createdGroups.Add(group);

                    var teamsInGroup = teamIds
                        .Skip(i * dto.TeamsPerGroup)
                        .Take(dto.TeamsPerGroup)
                        .ToList();

                    foreach (var teamId in teamsInGroup)
                    {
                        createdGroupTeams.Add(new GroupTeam
                        {
                            Group = group,
                            TeamId = teamId,
                            JoinedAt = DateTime.UtcNow
                        });
                    }

                    groupNameChar++;
                }
            }

            // 5️⃣ Lưu tất cả GroupTeam mới
            foreach (var gt in createdGroupTeams)
                await _uow.GroupsTeams.AddAsync(gt);

            await _uow.SaveAsync();

            // 6️⃣ Map DTO
            var result = _mapper.Map<List<GroupDto>>(createdGroups);

            foreach (var g in result)
            {
                g.TeamIds = createdGroupTeams
                    .Where(x => x.GroupId == g.GroupId)
                    .Select(x => x.TeamId)
                    .ToList();
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