using AutoMapper;
using Common.DTOs.GroupDto;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Servicefolder;

namespace NUniTest.Service
{
    [TestFixture]
    public class GroupServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private GroupService _service;

        private Mock<IRepository<Track>> _trackRepo;
        private Mock<IRepository<Group>> _groupRepo;
        private Mock<IRepository<GroupTeam>> _groupTeamRepo;
        private Mock<IRepository<TeamTrackSelection>> _teamTrackSelectionRepo;
        private Mock<IRepository<HackathonPhase>> _hackathonPhaseRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _trackRepo = new Mock<IRepository<Track>>();
            _groupRepo = new Mock<IRepository<Group>>();
            _groupTeamRepo = new Mock<IRepository<GroupTeam>>();
            _teamTrackSelectionRepo = new Mock<IRepository<TeamTrackSelection>>();
            _hackathonPhaseRepo = new Mock<IRepository<HackathonPhase>>();

            _uowMock.Setup(u => u.Tracks).Returns(_trackRepo.Object);
            _uowMock.Setup(u => u.Groups).Returns(_groupRepo.Object);
            _uowMock.Setup(u => u.GroupsTeams).Returns(_groupTeamRepo.Object);
            _uowMock.Setup(u => u.TeamTrackSelections).Returns(_teamTrackSelectionRepo.Object);
            _uowMock.Setup(u => u.HackathonPhases).Returns(_hackathonPhaseRepo.Object);

            _service = new GroupService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // 1. CreateGroupsByTrackAsync - TeamsPerGroup <= 0
        // =============================
        [Test]
        public void CreateGroupsByTrackAsync_WhenTeamsPerGroupInvalid_Throws()
        {
            var dto = new CreateGroupsRequestDto
            {
                PhaseId = 1,
                TeamsPerGroup = 0
            };

            Func<Task> act = async () => await _service.CreateGroupsByTrackAsync(dto);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("TeamsPerGroup must be greater than 0");
        }

        // =============================
        // 2. CreateGroupsByTrackAsync - No tracks found for phase
        // =============================
        [Test]
        public void CreateGroupsByTrackAsync_WhenNoTracksFound_Throws()
        {
            var dto = new CreateGroupsRequestDto
            {
                PhaseId = 1,
                TeamsPerGroup = 2
            };

            _trackRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Track, bool>>>(), null, null))
                .ReturnsAsync(new List<Track>());

            Func<Task> act = async () => await _service.CreateGroupsByTrackAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("No tracks found for this phase.");
        }

        // =============================
        // 3. CreateGroupsByTrackAsync - No teams selected tracks
        // =============================
        [Test]
        public void CreateGroupsByTrackAsync_WhenNoTeamsSelected_Throws()
        {
            var dto = new CreateGroupsRequestDto
            {
                PhaseId = 1,
                TeamsPerGroup = 2
            };
            var tracks = new List<Track>
            {
                new Track { TrackId = 1, PhaseId = 1 },
                new Track { TrackId = 2, PhaseId = 1 }
            };

            _trackRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Track, bool>>>(), null, null))
                .ReturnsAsync(tracks);
            _groupRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Group, bool>>>(), null, null))
                .ReturnsAsync(new List<Group>());
            _teamTrackSelectionRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamTrackSelection, bool>>>(), null, null))
                .ReturnsAsync(new List<TeamTrackSelection>());

            Func<Task> act = async () => await _service.CreateGroupsByTrackAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("No teams selected tracks in this phase.");
        }

      
        // =============================
        // 6. GetGroupsByHackathonAsync - No phases found
        // =============================
        [Test]
        public async Task GetGroupsByHackathonAsync_WhenNoPhasesFound_ReturnsEmptyList()
        {
            _hackathonPhaseRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<HackathonPhase, bool>>>(), null, null))
                .ReturnsAsync(new List<HackathonPhase>());

            var result = await _service.GetGroupsByHackathonAsync(1);

            result.Should().BeEmpty();
        }

        // =============================
        // 7. GetGroupsByHackathonAsync - No tracks found
        // =============================
        [Test]
        public async Task GetGroupsByHackathonAsync_WhenNoTracksFound_ReturnsEmptyList()
        {
            var phases = new List<HackathonPhase>
            {
                new HackathonPhase { PhaseId = 1, HackathonId = 1 }
            };

            _hackathonPhaseRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<HackathonPhase, bool>>>(), null, null))
                .ReturnsAsync(phases);
            _trackRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Track, bool>>>(), null, null))
                .ReturnsAsync(new List<Track>());

            var result = await _service.GetGroupsByHackathonAsync(1);

            result.Should().BeEmpty();
        }

        // =============================
        // 8. GetGroupsByHackathonAsync - No groups found
        // =============================
        [Test]
        public async Task GetGroupsByHackathonAsync_WhenNoGroupsFound_ReturnsEmptyList()
        {
            var phases = new List<HackathonPhase>
            {
                new HackathonPhase { PhaseId = 1, HackathonId = 1 }
            };
            var tracks = new List<Track>
            {
                new Track { TrackId = 1, PhaseId = 1 }
            };

            _hackathonPhaseRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<HackathonPhase, bool>>>(), null, null))
                .ReturnsAsync(phases);
            _trackRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Track, bool>>>(), null, null))
                .ReturnsAsync(tracks);
            _groupRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Group, bool>>>(), null, null))
                .ReturnsAsync(new List<Group>());

            var result = await _service.GetGroupsByHackathonAsync(1);

            result.Should().BeEmpty();
        }

       
        // =============================
        // 10. GetGroupTeamsByGroupIdAsync - Group not found
        // =============================
        [Test]
        public void GetGroupTeamsByGroupIdAsync_WhenGroupNotFound_Throws()
        {
            _groupRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Group)null);

            Func<Task> act = async () => await _service.GetGroupTeamsByGroupIdAsync(99);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("GroupId không tồn tại");
        }

        // =============================
        // 11. GetGroupTeamsByGroupIdAsync - Return group teams successfully
        // =============================
        [Test]
        public async Task GetGroupTeamsByGroupIdAsync_WhenGroupExists_ReturnsGroupTeams()
        {
            var group = new Group { GroupId = 1, GroupName = "A" };
            var groupTeams = new List<GroupTeam>
            {
                new GroupTeam 
                { 
                    GroupId = 1, 
                    TeamId = 1,
                    Team = new Team { TeamId = 1, TeamName = "Team 1" }
                },
                new GroupTeam 
                { 
                    GroupId = 1, 
                    TeamId = 2,
                    Team = new Team { TeamId = 2, TeamName = "Team 2" }
                }
            };
            var groupTeamDtos = new List<GroupTeamDto>
            {
                new GroupTeamDto { GroupId = 1, TeamId = 1, TeamName = "Team 1" },
                new GroupTeamDto { GroupId = 1, TeamId = 2, TeamName = "Team 2" }
            };

            _groupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);
            _groupTeamRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<GroupTeam, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<GroupTeam, object>>[]>()))
                .ReturnsAsync(groupTeams);
            _mapperMock.Setup(m => m.Map<List<GroupTeamDto>>(groupTeams)).Returns(groupTeamDtos);

            var result = await _service.GetGroupTeamsByGroupIdAsync(1);

            result.Should().HaveCount(2);
            result.First().TeamName.Should().Be("Team 1");
            result.Last().TeamName.Should().Be("Team 2");
        }

       
    }
}