using AutoMapper;
using Common.DTOs.QualifiedFinealTeamDto;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using Service.Servicefolder;
using System.Linq.Expressions;

namespace NUniTest.Service
{
    [TestFixture]
    public class QualificationServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private QualificationService _service;

        private Mock<IRepository<HackathonPhase>> _phaseRepo;
        private Mock<IRepository<Group>> _groupRepo;
        private Mock<IRepository<GroupTeam>> _groupTeamRepo;
        private Mock<IRepository<FinalQualification>> _finalQualificationRepo;
        private Mock<IRepository<PenaltiesBonuse>> _penaltyBonusRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _phaseRepo = new Mock<IRepository<HackathonPhase>>();
            _groupRepo = new Mock<IRepository<Group>>();
            _groupTeamRepo = new Mock<IRepository<GroupTeam>>();
            _finalQualificationRepo = new Mock<IRepository<FinalQualification>>();
            _penaltyBonusRepo = new Mock<IRepository<PenaltiesBonuse>>();

            _uowMock.Setup(u => u.HackathonPhases).Returns(_phaseRepo.Object);
            _uowMock.Setup(u => u.Groups).Returns(_groupRepo.Object);
            _uowMock.Setup(u => u.GroupsTeams).Returns(_groupTeamRepo.Object);
            _uowMock.Setup(u => u.FinalQualifications).Returns(_finalQualificationRepo.Object);
            _uowMock.Setup(u => u.PenaltiesBonuses).Returns(_penaltyBonusRepo.Object);

            _service = new QualificationService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // GenerateQualifiedTeamsAsync Tests
        // =============================

        [Test]
        public async Task GenerateQualifiedTeamsAsync_WhenPhaseNotFound_ShouldReturnEmpty()
        {
            // Arrange
            _phaseRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((HackathonPhase)null);

            // Act
            var result = await _service.GenerateQualifiedTeamsAsync(999);

            // Assert
            result.Should().BeEmpty();
        }

     

        [Test]
        public async Task GenerateQualifiedTeamsAsync_WhenNoScoringPhase_ShouldReturnEmpty()
        {
            // Arrange
            var currentPhase = new HackathonPhase { PhaseId = 1, HackathonId = 1 };
            var emptyPhases = new List<HackathonPhase>();

            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(currentPhase);
            _phaseRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<HackathonPhase, bool>>>(),
        It.IsAny<Func<IQueryable<HackathonPhase>, IOrderedQueryable<HackathonPhase>>>(),
        It.IsAny<string>())).ReturnsAsync(emptyPhases);

            // Act
            var result = await _service.GenerateQualifiedTeamsAsync(1);

            // Assert
            result.Should().BeEmpty();
        }

        // =============================
        // GetFinalQualifiedTeamsAsync Tests
        // =============================

        [Test]
        public void GetFinalQualifiedTeamsAsync_WhenPhaseNotFound_Throws()
        {
            // Arrange
            _phaseRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((HackathonPhase)null);

            // Act & Assert
            Func<Task> act = async () => await _service.GetFinalQualifiedTeamsAsync(999);
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Test]
        public void GetFinalQualifiedTeamsAsync_WhenNoPhases_Throws()
        {
            // Arrange
            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 1 };
            var emptyPhases = new List<HackathonPhase>();

            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);
            _phaseRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<HackathonPhase, bool>>>(),
        It.IsAny<Func<IQueryable<HackathonPhase>, IOrderedQueryable<HackathonPhase>>>(),
        It.IsAny<string>())).ReturnsAsync(emptyPhases);

            // Act & Assert
            Func<Task> act = async () => await _service.GetFinalQualifiedTeamsAsync(1);
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public void GetFinalQualifiedTeamsAsync_WhenNotFinalPhase_Throws()
        {
            // Arrange
            var inputPhase = new HackathonPhase { PhaseId = 1, HackathonId = 1, EndDate = DateTime.UtcNow };
            var finalPhase = new HackathonPhase { PhaseId = 2, HackathonId = 1, EndDate = DateTime.UtcNow.AddDays(1) };
            var phases = new List<HackathonPhase> { inputPhase, finalPhase };

            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inputPhase);
            _phaseRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<HackathonPhase, bool>>>(),
        It.IsAny<Func<IQueryable<HackathonPhase>, IOrderedQueryable<HackathonPhase>>>(),
        It.IsAny<string>())).ReturnsAsync(phases);

            // Act & Assert
            Func<Task> act = async () => await _service.GetFinalQualifiedTeamsAsync(1);
            act.Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task GetFinalQualifiedTeamsAsync_WhenValid_ShouldReturnQualifiedTeams()
        {
            // Arrange
            var finalPhase = new HackathonPhase { PhaseId = 2, HackathonId = 1, EndDate = DateTime.UtcNow.AddDays(1) };
            var inputPhase = new HackathonPhase { PhaseId = 2, HackathonId = 1, EndDate = DateTime.UtcNow.AddDays(1) };
            var phases = new List<HackathonPhase> { inputPhase };

            var team = new Team { TeamId = 1, TeamName = "Team A", HackathonId = 1 };
            var group = new Group { GroupId = 1, GroupName = "Group 1" };
            var track = new Track { TrackId = 1, Name = "Track 1" };
            var finals = new List<FinalQualification>
            {
                new FinalQualification { TeamId = 1, GroupId = 1, Team = team, Group = group, Track = track }
            };

            _phaseRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(finalPhase);
            _phaseRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<HackathonPhase, bool>>>(),
        It.IsAny<Func<IQueryable<HackathonPhase>, IOrderedQueryable<HackathonPhase>>>(),
        It.IsAny<string>())).ReturnsAsync(phases);

            _finalQualificationRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<FinalQualification, bool>>>(),
                It.IsAny<Expression<Func<FinalQualification, object>>>(),
                It.IsAny<Expression<Func<FinalQualification, object>>>(),
                It.IsAny<Expression<Func<FinalQualification, object>>>()))
                .ReturnsAsync(finals);

            // Act
            var result = await _service.GetFinalQualifiedTeamsAsync(2);

            // Assert
            result.Should().NotBeEmpty();
            result.First().TeamId.Should().Be(1);
            result.First().TeamName.Should().Be("Team A");
        }
    }
}
