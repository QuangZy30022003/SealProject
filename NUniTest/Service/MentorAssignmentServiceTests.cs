using AutoMapper;
using Common.DTOs.AssignedTeamDto;
using Common.DTOs.NotificationDto;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
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
using System.Threading.Tasks;

namespace NUniTest.Service
{
    [TestFixture]
    public class MentorAssignmentServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IEmailService> _emailServiceMock;
        private Mock<IConfiguration> _configMock;
        private Mock<INotificationService> _notificationServiceMock;
        private MentorAssignmentService _service;

        private Mock<IRepository<MentorAssignment>> _mentorAssignmentRepo;
        private Mock<IRepository<User>> _userRepo;
        private Mock<IRepository<Team>> _teamRepo;
        private Mock<IRepository<Hackathon>> _hackathonRepo;
        private Mock<IRepository<TeamMember>> _teamMemberRepo;
        private Mock<IRepository<MentorVerification>> _mentorVerificationRepo;
        private Mock<IRepository<HackathonRegistration>> _registrationRepo;
        private Mock<IRepository<ChatGroup>> _chatGroupRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _emailServiceMock = new Mock<IEmailService>();
            _configMock = new Mock<IConfiguration>();
            _notificationServiceMock = new Mock<INotificationService>();

            _mentorAssignmentRepo = new Mock<IRepository<MentorAssignment>>();
            _userRepo = new Mock<IRepository<User>>();
            _teamRepo = new Mock<IRepository<Team>>();
            _hackathonRepo = new Mock<IRepository<Hackathon>>();
            _teamMemberRepo = new Mock<IRepository<TeamMember>>();
            _mentorVerificationRepo = new Mock<IRepository<MentorVerification>>();
            _registrationRepo = new Mock<IRepository<HackathonRegistration>>();
            _chatGroupRepo = new Mock<IRepository<ChatGroup>>();

            _uowMock.Setup(u => u.MentorAssignments).Returns(_mentorAssignmentRepo.Object);
            _uowMock.Setup(u => u.Users).Returns(_userRepo.Object);
            _uowMock.Setup(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.Setup(u => u.Hackathons).Returns(_hackathonRepo.Object);
            _uowMock.Setup(u => u.TeamMembers).Returns(_teamMemberRepo.Object);
            _uowMock.Setup(u => u.MentorVerifications).Returns(_mentorVerificationRepo.Object);
            _uowMock.Setup(u => u.HackathonRegistrations).Returns(_registrationRepo.Object);
            _uowMock.Setup(u => u.ChatGroups).Returns(_chatGroupRepo.Object);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _service = new MentorAssignmentService(
                _uowMock.Object,
                _mapperMock.Object,
                _emailServiceMock.Object,
                _configMock.Object,
                _notificationServiceMock.Object);
        }

        [Test]
        public async Task ViewAssignedTeamsAsync_ShouldReturnAssignedTeams()
        {
            // Arrange
            var mentorId = 1;
            var assignments = new List<MentorAssignment>
            {
                new MentorAssignment { AssignmentId = 1, MentorId = mentorId, TeamId = 10, Status = "Approved", AssignedAt = DateTime.UtcNow, Team = new Team { TeamId = 10, TeamName = "Team A", TeamLeaderId = 5, TeamLeader = new User { FullName = "Leader A" } } },
                new MentorAssignment { AssignmentId = 2, MentorId = mentorId, TeamId = 20, Status = "Approved", AssignedAt = DateTime.UtcNow, Team = new Team { TeamId = 20, TeamName = "Team B", TeamLeaderId = 6, TeamLeader = new User { FullName = "Leader B" } } }
            };

            var mockRepo = new Mock<IMentorAssignmentRepository>();
            mockRepo.Setup(r => r.GetTeamsByMentorIdAsync(mentorId)).ReturnsAsync(assignments);
            _uowMock.Setup(u => u.MentorAssignmentRepository).Returns(mockRepo.Object);

            // Act
            var result = await _service.ViewAssignedTeamsAsync(mentorId);

            // Assert
            result.Should().HaveCount(2);
            result.First().TeamName.Should().Be("Team A");
            result.Last().TeamName.Should().Be("Team B");
        }

        [Test]
        public void RegisterAsync_WhenUserNotTeamMember_Throws()
        {
            // Arrange
            var userId = 1;
            var dto = new MentorAssignmentCreateDto { TeamId = 10, MentorId = 2, HackathonId = 1 };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                .ReturnsAsync((TeamMember)null);

            // Act & Assert
            Func<Task> act = async () => await _service.RegisterAsync(userId, dto);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not a member of this team!");
        }

        [Test]
        public void RegisterAsync_WhenUserNotTeamLeader_Throws()
        {
            // Arrange
            var userId = 1;
            var dto = new MentorAssignmentCreateDto { TeamId = 10, MentorId = 2, HackathonId = 1 };
            var teamMember = new TeamMember { UserId = userId, TeamId = 10, RoleInTeam = "Member" };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                .ReturnsAsync(teamMember);

            // Act & Assert
            Func<Task> act = async () => await _service.RegisterAsync(userId, dto);
            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only TeamLeader or Leader can register a mentor!");
        }

        [Test]
        public void RegisterAsync_WhenMentorNotFound_Throws()
        {
            // Arrange
            var userId = 1;
            var dto = new MentorAssignmentCreateDto { TeamId = 10, MentorId = 999, HackathonId = 1 };
            var teamMember = new TeamMember { UserId = userId, TeamId = 10, RoleInTeam = "TeamLeader" };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                .ReturnsAsync(teamMember);
            _userRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User)null);

            // Act & Assert
            Func<Task> act = async () => await _service.RegisterAsync(userId, dto);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("Invalid mentor!");
        }

        [Test]
        public void RegisterAsync_WhenMentorNotVerified_Throws()
        {
            // Arrange
            var userId = 1;
            var dto = new MentorAssignmentCreateDto { TeamId = 10, MentorId = 2, HackathonId = 1 };
            var teamMember = new TeamMember { UserId = userId, TeamId = 10, RoleInTeam = "TeamLeader" };
            var mentor = new User { UserId = 2, RoleId = 5, Email = "mentor@test.com" };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                .ReturnsAsync(teamMember);
            _userRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(mentor);
            _mentorVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<MentorVerification, bool>>>()))
                .ReturnsAsync((MentorVerification)null);

            // Act & Assert
            Func<Task> act = async () => await _service.RegisterAsync(userId, dto);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("This mentor is not verified for this hackathon!");
        }

        [Test]
        public void RegisterAsync_WhenTeamNotRegistered_Throws()
        {
            // Arrange
            var userId = 1;
            var dto = new MentorAssignmentCreateDto { TeamId = 10, MentorId = 2, HackathonId = 1 };
            var teamMember = new TeamMember { UserId = userId, TeamId = 10, RoleInTeam = "TeamLeader" };
            var mentor = new User { UserId = 2, RoleId = 5, Email = "mentor@test.com" };
            var verification = new MentorVerification { UserId = 2, HackathonId = 1, Status = "Approved" };
            var team = new Team { TeamId = 10, TeamName = "Team A" };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                .ReturnsAsync(teamMember);
            _userRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(mentor);
            _mentorVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<MentorVerification, bool>>>()))
                .ReturnsAsync(verification);
            _teamRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(team);
            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Hackathon { HackathonId = 1 });
            _registrationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync((HackathonRegistration)null);

            // Act & Assert
            Func<Task> act = async () => await _service.RegisterAsync(userId, dto);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team has not registered for this hackathon!");
        }

        [Test]
        public async Task RegisterAsync_WhenValid_ShouldCreateAssignment()
        {
            // Arrange
            var userId = 1;
            var dto = new MentorAssignmentCreateDto { TeamId = 10, MentorId = 2, HackathonId = 1 };
            var teamMember = new TeamMember { UserId = userId, TeamId = 10, RoleInTeam = "TeamLeader" };
            var mentor = new User { UserId = 2, RoleId = 5, Email = "mentor@test.com" };
            var verification = new MentorVerification { UserId = 2, HackathonId = 1, Status = "Approved" };
            var team = new Team { TeamId = 10, TeamName = "Team A" };
            var registration = new HackathonRegistration { TeamId = 10, HackathonId = 1, Status = "WaitingMentor" };
            var assignment = new MentorAssignment { AssignmentId = 1, MentorId = 2, TeamId = 10, HackathonId = 1, Status = "WaitingMentor" };
            var responseDto = new MentorAssignmentResponseDto { AssignmentId = 1, Status = "WaitingMentor" };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                .ReturnsAsync(teamMember);
            _userRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(mentor);
            _mentorVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<MentorVerification, bool>>>()))
                .ReturnsAsync(verification);
            _teamRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(team);
            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Hackathon { HackathonId = 1 });
            _registrationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync(registration);
            _mentorAssignmentRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<MentorAssignment, bool>>>(), null, null))
                .ReturnsAsync(new List<MentorAssignment>());
            _mentorAssignmentRepo.Setup(r => r.AddAsync(It.IsAny<MentorAssignment>())).Returns(Task.CompletedTask);
            _mapperMock.Setup(m => m.Map<MentorAssignmentResponseDto>(It.IsAny<MentorAssignment>())).Returns(responseDto);

            // Act
            var result = await _service.RegisterAsync(userId, dto);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("WaitingMentor");
            _mentorAssignmentRepo.Verify(r => r.AddAsync(It.IsAny<MentorAssignment>()), Times.Once);
        }

        [Test]
        public void ApproveAsync_WhenAssignmentNotFound_Throws()
        {
            // Arrange
            var assignmentId = 999;
            _mentorAssignmentRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<MentorAssignment, bool>>>(),
                It.IsAny<Expression<Func<MentorAssignment, object>>>(),
                It.IsAny<Expression<Func<MentorAssignment, object>>>(),
                It.IsAny<Expression<Func<MentorAssignment, object>>>()))
                .ReturnsAsync((MentorAssignment)null);

            // Act & Assert
            Func<Task> act = async () => await _service.ApproveAsync(assignmentId);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("Assignment not found");
        }

        [Test]
        public async Task ApproveAsync_WhenValid_ShouldApproveAndCreateChatGroup()
        {
            // Arrange
            var assignmentId = 1;
            var assignment = new MentorAssignment
            {
                AssignmentId = assignmentId,
                MentorId = 2,
                TeamId = 10,
                HackathonId = 1,
                Status = "WaitingMentor",
                Mentor = new User { UserId = 2, FullName = "Mentor A" },
                Team = new Team { TeamId = 10, TeamName = "Team A", TeamLeaderId = 5, TeamLeader = new User { UserId = 5, Email = "leader@test.com", FullName = "Leader A" } },
                Hackathon = new Hackathon { HackathonId = 1, Name = "Hackathon 1" }
            };
            var registration = new HackathonRegistration { TeamId = 10, HackathonId = 1, Status = "WaitingMentor", Team = assignment.Team, Hackathon = assignment.Hackathon };
            var teamMembers = new List<TeamMember> { new TeamMember { UserId = 5, TeamId = 10 }, new TeamMember { UserId = 6, TeamId = 10 } };
            var responseDto = new MentorAssignmentResponseDto { AssignmentId = assignmentId, Status = "Approved" };

            _mentorAssignmentRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<MentorAssignment, bool>>>(),
                It.IsAny<Expression<Func<MentorAssignment, object>>>(),
                It.IsAny<Expression<Func<MentorAssignment, object>>>(),
                It.IsAny<Expression<Func<MentorAssignment, object>>>()))
                .ReturnsAsync(assignment);
            _registrationRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<HackathonRegistration, bool>>>(),
                It.IsAny<Expression<Func<HackathonRegistration, object>>>(),
                It.IsAny<Expression<Func<HackathonRegistration, object>>>()))
                .ReturnsAsync(registration);
            _teamMemberRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<TeamMember, bool>>>(), null, null))
                .ReturnsAsync(teamMembers);
            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _chatGroupRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ChatGroup, bool>>>()))
                .ReturnsAsync((ChatGroup)null);
            _chatGroupRepo.Setup(r => r.AddAsync(It.IsAny<ChatGroup>())).Returns(Task.CompletedTask);
            _mapperMock.Setup(m => m.Map<MentorAssignmentResponseDto>(It.IsAny<MentorAssignment>())).Returns(responseDto);

            // Act
            var result = await _service.ApproveAsync(assignmentId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Approved");
            _mentorAssignmentRepo.Verify(r => r.Update(It.IsAny<MentorAssignment>()), Times.Once);
            _registrationRepo.Verify(r => r.Update(It.IsAny<HackathonRegistration>()), Times.Once);
        }

        [Test]
        public void RejectAsync_WhenAssignmentNotFound_Throws()
        {
            // Arrange
            var assignmentId = 999;
            _mentorAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync((MentorAssignment)null);

            // Act & Assert
            Func<Task> act = async () => await _service.RejectAsync(assignmentId);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("Assignment not found");
        }

        [Test]
        public async Task RejectAsync_WhenValid_ShouldRejectAndSendNotification()
        {
            // Arrange
            var assignmentId = 1;
            var assignment = new MentorAssignment
            {
                AssignmentId = assignmentId,
                MentorId = 2,
                TeamId = 10,
                HackathonId = 1,
                Status = "WaitingMentor"
            };
            var registration = new HackathonRegistration { TeamId = 10, HackathonId = 1, Status = "WaitingMentor" };
            var team = new Team { TeamId = 10, TeamName = "Team A", TeamLeaderId = 5, TeamLeader = new User { UserId = 5, Email = "leader@test.com", FullName = "Leader A" } };
            var responseDto = new MentorAssignmentResponseDto { AssignmentId = assignmentId, Status = "Rejected" };

            _mentorAssignmentRepo.Setup(r => r.GetByIdAsync(assignmentId)).ReturnsAsync(assignment);
            _registrationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<HackathonRegistration, bool>>>()))
                .ReturnsAsync(registration);
            _teamRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(team);
            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto());
            _mapperMock.Setup(m => m.Map<MentorAssignmentResponseDto>(It.IsAny<MentorAssignment>())).Returns(responseDto);

            // Act
            var result = await _service.RejectAsync(assignmentId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("Rejected");
            _mentorAssignmentRepo.Verify(r => r.Update(It.IsAny<MentorAssignment>()), Times.Once);
            _registrationRepo.Verify(r => r.Update(It.IsAny<HackathonRegistration>()), Times.Once);
        }

        [Test]
        public async Task GetByMentorAsync_ShouldReturnMentorAssignments()
        {
            // Arrange
            var mentorId = 1;
            var assignments = new List<MentorAssignment>
            {
                new MentorAssignment { AssignmentId = 1, MentorId = mentorId, TeamId = 10, Status = "Approved", Team = new Team { TeamName = "Team A" } },
                new MentorAssignment { AssignmentId = 2, MentorId = mentorId, TeamId = 20, Status = "Approved", Team = new Team { TeamName = "Team B" } }
            };
            var dtos = new List<MentorAssignmentResponseDto>
            {
                new MentorAssignmentResponseDto { AssignmentId = 1, Status = "Approved" },
                new MentorAssignmentResponseDto { AssignmentId = 2, Status = "Approved" }
            };

            _mentorAssignmentRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<MentorAssignment, bool>>>(), null, "Team"))
                .ReturnsAsync(assignments);
            _mapperMock.Setup(m => m.Map<IEnumerable<MentorAssignmentResponseDto>>(assignments)).Returns(dtos);

            // Act
            var result = await _service.GetByMentorAsync(mentorId);

            // Assert
            result.Should().HaveCount(2);
            result.First().Status.Should().Be("Approved");
        }
    }
}
