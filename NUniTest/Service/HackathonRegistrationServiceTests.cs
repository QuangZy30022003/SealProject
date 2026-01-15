using AutoMapper;
using Common.DTOs.NotificationDto;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using Service.Servicefolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NUniTest.Service
{
    [TestFixture]
    public class HackathonRegistrationServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<INotificationService> _notificationMock;

        private Mock<IRepository<HackathonRegistration>> _regRepo;
        private Mock<IRepository<Team>> _teamRepo;
        private Mock<IRepository<Hackathon>> _hackathonRepo;
        private Mock<IRepository<Chapter>> _chapterRepo;

        private HackathonRegistrationService _service;

        [SetUp]
        public void SetUp()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _notificationMock = new Mock<INotificationService>();

            _regRepo = new Mock<IRepository<HackathonRegistration>>();
            _teamRepo = new Mock<IRepository<Team>>();
            _hackathonRepo = new Mock<IRepository<Hackathon>>();
            _chapterRepo = new Mock<IRepository<Chapter>>();

            _uowMock.Setup(u => u.HackathonRegistrations).Returns(_regRepo.Object);
            _uowMock.Setup(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.Setup(u => u.Hackathons).Returns(_hackathonRepo.Object);
            _uowMock.Setup(u => u.Chapters).Returns(_chapterRepo.Object);

            _service = new HackathonRegistrationService(
                _uowMock.Object,
                _mapperMock.Object,
                _notificationMock.Object
            );
        }

        // -------------------------------------------------------------
        // REGISTER TEAM
        // -------------------------------------------------------------

        [Test]
        public async Task RegisterTeamAsync_ShouldReturnError_WhenTeamNotFound()
        {
            _teamRepo.Setup(r =>
                r.GetByIdIncludingAsync(It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<Expression<Func<Team, object>>[]>())
            ).ReturnsAsync((Team)null);

            var result = await _service.RegisterTeamAsync(1, 1, 1, "link");

            result.Should().Be("Team not found.");
        }

        [Test]
        public async Task RegisterTeamAsync_ShouldReturnError_WhenUserIsNotLeader()
        {
            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 99,
                TeamMembers = new List<TeamMember>()
            };

            _teamRepo.Setup(r =>
                r.GetByIdIncludingAsync(It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<Expression<Func<Team, object>>[]>()))
                .ReturnsAsync(team);

            var result = await _service.RegisterTeamAsync(1, 1, 1, "");

            result.Should().Be("Only the Team Leader can register the team for the hackathon.");
        }

        [Test]
        public async Task RegisterTeamAsync_ShouldReturnError_WhenTeamAlreadyBound()
        {
            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 1,
                HackathonId = 5,
                TeamMembers = new List<TeamMember> { new(), new(), new() }
            };

            _teamRepo.Setup(r =>
                r.GetByIdIncludingAsync(It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<Expression<Func<Team, object>>[]>()))
                .ReturnsAsync(team);

            var result = await _service.RegisterTeamAsync(1, 1, 1, "");

            result.Should().Be("This team is already bound to a hackathon.");
        }

        [Test]
        public async Task RegisterTeamAsync_ShouldReturnError_WhenHackathonNotFound()
        {
            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 1,
                HackathonId = null,
                TeamMembers = new List<TeamMember> { new(), new(), new() }
            };

            _teamRepo.Setup(r =>
                r.GetByIdIncludingAsync(It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<Expression<Func<Team, object>>[]>()))
                .ReturnsAsync(team);

            _hackathonRepo.Setup(r =>
                r.ExistsAsync(It.IsAny<Expression<Func<Hackathon, bool>>>()))
                .ReturnsAsync(false);

            var result = await _service.RegisterTeamAsync(1, 2, 1, "");

            result.Should().Be("Hackathon not found.");
        }

        [Test]
        public async Task RegisterTeamAsync_ShouldReturnSuccess_WhenValid()
        {
            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 1,
                HackathonId = null,
                TeamMembers = new List<TeamMember> { new(), new(), new() }
            };

            _teamRepo.Setup(r =>
                r.GetByIdIncludingAsync(It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<Expression<Func<Team, object>>[]>()))
                .ReturnsAsync(team);

            _hackathonRepo.Setup(r =>
                r.ExistsAsync(It.IsAny<Expression<Func<Hackathon, bool>>>()))
                .ReturnsAsync(true);

            _teamRepo.Setup(r =>
                r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(false);

            _regRepo.Setup(r =>
                r.ExistsAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync(false);

            var result = await _service.RegisterTeamAsync(1, 1, 1, "link");

            result.Should().Be("Team successfully registered for the hackathon.");

            _regRepo.Verify(r => r.AddAsync(It.IsAny<HackathonRegistration>()), Times.Once);
            _teamRepo.Verify(r => r.Update(team), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()),Times.Once);


        }

        // -------------------------------------------------------------
        // CANCEL REGISTRATION
        // -------------------------------------------------------------

        [Test]
        public async Task CancelRegistrationAsync_ShouldReturnError_WhenNotRegistered()
        {
            _regRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync((HackathonRegistration)null);

            var result = await _service.CancelRegistrationAsync(1, 1, "reason", 1);

            result.Should().Be("Team has not registered for this hackathon.");
        }

        [Test]
        public async Task CancelRegistrationAsync_ShouldReturnError_WhenUserIsNotLeader()
        {
            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 99,
                TeamMembers = new List<TeamMember>()
            };

            var registration = new HackathonRegistration
            {
                TeamId = 1,
                HackathonId = 1,
                Status = "Pending",
                Team = team
            };

            _regRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync(registration);

            _teamRepo.Setup(r =>
                r.GetByIdIncludingAsync(It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<Expression<Func<Team, object>>[]>()))
                .ReturnsAsync(team);

            var result = await _service.CancelRegistrationAsync(1, 1, "reason", 1);

            result.Should().Be("Only the Team Leader can cancel the registration.");
        }

        [Test]
        public async Task CancelRegistrationAsync_ShouldReturnSuccess_WhenValid()
        {
            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 1,
                TeamMembers = new List<TeamMember>()
            };

            var registration = new HackathonRegistration
            {
                TeamId = 1,
                HackathonId = 1,
                Status = "Pending",
                Team = team
            };

            _regRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync(registration);

            _teamRepo.Setup(r =>
                r.GetByIdIncludingAsync(It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<Expression<Func<Team, object>>[]>()))
                .ReturnsAsync(team);

            var result = await _service.CancelRegistrationAsync(1, 1, "reason", 1);

            result.Should().Be("Team registration has been cancelled.");
            registration.Status.Should().Be("Cancelled");

            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Once);
        }

        // -------------------------------------------------------------
        // APPROVE TEAM
        // -------------------------------------------------------------

        [Test]
        public async Task ApproveTeamAsync_ShouldReturnError_WhenNotRegistered()
        {
            _regRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync((HackathonRegistration)null);

            var result = await _service.ApproveTeamAsync(100, 1, 1);

            result.Should().Be("Team has not registered for this hackathon.");
        }

        [Test]
        public async Task ApproveTeamAsync_ShouldReturnError_WhenUnauthorized()
        {
            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 1,
                ChapterId = 10
            };

            var chapter = new Chapter
            {
                ChapterId = 10,
                ChapterLeaderId = 999
            };

            var reg = new HackathonRegistration
            {
                TeamId = 1,
                HackathonId = 1,
                Status = "Pending"
            };

            _regRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync(reg);

            _teamRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);

            _chapterRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Chapter, bool>>>()))
                .ReturnsAsync(chapter);

            var result = await _service.ApproveTeamAsync(100, 1, 1);

            result.Should().Be("You are not authorized to approve teams outside your chapter.");
        }

        [Test]
        public async Task ApproveTeamAsync_ShouldReturnSuccess_WhenValid()
        {
            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 1,
                ChapterId = 10
            };

            var chapter = new Chapter
            {
                ChapterId = 10,
                ChapterLeaderId = 100
            };

            var reg = new HackathonRegistration
            {
                TeamId = 1,
                HackathonId = 1,
                Status = "Pending"
            };

            _regRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync(reg);

            _teamRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);

            _chapterRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Chapter, bool>>>()))
                .ReturnsAsync(chapter);

            _hackathonRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Hackathon
                {
                    HackathonId = 1,
                    Name = "Test Hackathon"
                });

            var result = await _service.ApproveTeamAsync(100, 1, 1);

            result.Should().Be("Team registration approved successfully.");
            reg.Status.Should().Be("WaitingMentor");

            _notificationMock.Verify(n =>
                n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()),
                Times.Once);

            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Once);
        }

        [Test]
        public async Task RejectTeamAsync_ShouldReturnError_WhenNotRegistered()
        {
            _regRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync((HackathonRegistration)null);

            var result = await _service.RejectTeamAsync(100, 1, 1, "reason");

            result.Should().Be("Team has not registered for this hackathon.");
        }

        [Test]
        public async Task RejectTeamAsync_ShouldReturnSuccess_WhenValid()
        {
            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 1,
                ChapterId = 10
            };

            var chapter = new Chapter
            {
                ChapterId = 10,
                ChapterLeaderId = 100
            };

            var reg = new HackathonRegistration
            {
                TeamId = 1,
                HackathonId = 1,
                Status = "Pending"
            };

            _regRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync(reg);

            _teamRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                .ReturnsAsync(team);

            _chapterRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Chapter, bool>>>()))
                .ReturnsAsync(chapter);

            _hackathonRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Hackathon
                {
                    HackathonId = 1,
                    Name = "Test Hackathon"
                });

            var result = await _service.RejectTeamAsync(100, 1, 1, "Invalid team");

            result.Should().Be("Team registration rejected successfully.");
            reg.Status.Should().Be("Rejected");

            _notificationMock.Verify(n =>
                n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()),
                Times.Once);

            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Once);
        }
    }
}
