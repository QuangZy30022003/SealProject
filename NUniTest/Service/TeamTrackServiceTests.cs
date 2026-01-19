using AutoMapper;
using Common.DTOs.TeamTrackDto;
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
    public class TeamTrackServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private TeamTrackService _service;

        private Mock<IRepository<Team>> _teamRepo;
        private Mock<IRepository<Track>> _trackRepo;
        private Mock<IRepository<TeamTrackSelection>> _teamTrackSelectionRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _teamRepo = new Mock<IRepository<Team>>();
            _trackRepo = new Mock<IRepository<Track>>();
            _teamTrackSelectionRepo = new Mock<IRepository<TeamTrackSelection>>();

            _uowMock.Setup(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.Setup(u => u.Tracks).Returns(_trackRepo.Object);
            _uowMock.Setup(u => u.TeamTrackSelections).Returns(_teamTrackSelectionRepo.Object);

            _service = new TeamTrackService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // 1. SelectTrackAsync - Team not found
        // =============================
        [Test]
        public void SelectTrackAsync_WhenTeamNotFound_Throws()
        {
            var request = new TeamSelectTrackRequest
            {
                TeamId = 99,
                TrackId = 1
            };

            _teamRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Team)null);

            Func<Task> act = async () => await _service.SelectTrackAsync(1, request);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team not found.");
        }

        // =============================
        // 2. SelectTrackAsync - Track not found
        // =============================
        [Test]
        public void SelectTrackAsync_WhenTrackNotFound_Throws()
        {
            var request = new TeamSelectTrackRequest
            {
                TeamId = 1,
                TrackId = 99
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _trackRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Track)null);

            Func<Task> act = async () => await _service.SelectTrackAsync(1, request);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Track not found.");
        }

        // =============================
        // 3. SelectTrackAsync - User not team leader
        // =============================
        [Test]
        public void SelectTrackAsync_WhenUserNotTeamLeader_Throws()
        {
            var request = new TeamSelectTrackRequest
            {
                TeamId = 1,
                TrackId = 1
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 2, TeamName = "Test Team" }; // Leader is user 2
            var track = new Track { TrackId = 1, Name = "Web Development" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);

            Func<Task> act = async () => await _service.SelectTrackAsync(1, request); // User 1 is not leader

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Only team leader can select track.");
        }

        // =============================
        // 4. SelectTrackAsync - Team already selected track
        // =============================
        [Test]
        public void SelectTrackAsync_WhenTeamAlreadySelectedTrack_Throws()
        {
            var request = new TeamSelectTrackRequest
            {
                TeamId = 1,
                TrackId = 1
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };
            var track = new Track { TrackId = 1, Name = "Web Development" };
            var existingSelection = new TeamTrackSelection
            {
                TeamId = 1,
                TrackId = 2,
                SelectedAt = DateTime.UtcNow.AddDays(-1)
            };
            var existingSelections = new List<TeamTrackSelection> { existingSelection };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);
            _teamTrackSelectionRepo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamTrackSelection, bool>>>(), null, null))
                .ReturnsAsync(existingSelections);

            Func<Task> act = async () => await _service.SelectTrackAsync(1, request);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team has already selected a track.");
        }

      
    }
}