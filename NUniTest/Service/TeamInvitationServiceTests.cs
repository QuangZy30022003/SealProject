using AutoMapper;
using Common.DTOs.NotificationDto;
using Common.DTOs.TeamInvitationDto;
using Common.Enums;
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
    public class TeamInvitationServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IEmailService> _emailServiceMock;
        private Mock<INotificationService> _notificationServiceMock;
        private TeamInvitationService _service;

        private Mock<IRepository<Team>> _teamRepo;
        private Mock<IRepository<TeamMember>> _teamMemberRepo;
        private Mock<IRepository<User>> _userRepo;
        private Mock<IRepository<TeamInvitation>> _teamInvitationRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _emailServiceMock = new Mock<IEmailService>();
            _notificationServiceMock = new Mock<INotificationService>();

            _teamRepo = new Mock<IRepository<Team>>();
            _teamMemberRepo = new Mock<IRepository<TeamMember>>();
            _userRepo = new Mock<IRepository<User>>();
            _teamInvitationRepo = new Mock<IRepository<TeamInvitation>>();

            _uowMock.Setup(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.Setup(u => u.TeamMembers).Returns(_teamMemberRepo.Object);
            _uowMock.Setup(u => u.Users).Returns(_userRepo.Object);
            _uowMock.Setup(u => u.TeamInvitations).Returns(_teamInvitationRepo.Object);

            _service = new TeamInvitationService(_uowMock.Object, _mapperMock.Object, _emailServiceMock.Object, _notificationServiceMock.Object);
        }

        // =============================
        // 1. InviteMemberAsync - Team not found
        // =============================
        [Test]
        public void InviteMemberAsync_WhenTeamNotFound_Throws()
        {
            _teamRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Team)null);

            Func<Task> act = async () => await _service.InviteMemberAsync(99, "test@email.com", 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team not found.");
        }

        // =============================
        // 2. InviteMemberAsync - User not team leader
        // =============================
        [Test]
        public void InviteMemberAsync_WhenUserNotTeamLeader_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 2, TeamName = "Test Team" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.InviteMemberAsync(1, "test@email.com", 1); // User 1 is not leader

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only team leader can invite members.");
        }

        // =============================
        // 3. InviteMemberAsync - Team already has maximum members
        // =============================
        [Test]
        public void InviteMemberAsync_WhenTeamFull_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(5); // Already 5 members

            Func<Task> act = async () => await _service.InviteMemberAsync(1, "test@email.com", 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team already has maximum number of members (5).");
        }

        // =============================
        // 4. InviteMemberAsync - User already in another unregistered team
        // =============================
        [Test]
        public void InviteMemberAsync_WhenUserInAnotherUnregisteredTeam_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team", HackathonId = null };
            var invitedUser = new User { UserId = 2, Email = "test@email.com" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(2);
            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>()))
                .ReturnsAsync(invitedUser);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(true); // User is in another unregistered team

            Func<Task> act = async () => await _service.InviteMemberAsync(1, "test@email.com", 1);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("User is already in another team that hasn't registered for any hackathon.");
        }

        // =============================
        // 5. InviteMemberAsync - User already in another team same hackathon
        // =============================
        [Test]
        public void InviteMemberAsync_WhenUserInAnotherTeamSameHackathon_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team", HackathonId = 1 };
            var invitedUser = new User { UserId = 2, Email = "test@email.com" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(2);
            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>()))
                .ReturnsAsync(invitedUser);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(true); // User is in another team in same hackathon

            Func<Task> act = async () => await _service.InviteMemberAsync(1, "test@email.com", 1);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("User is already in another team in this hackathon.");
        }

        // =============================
        // 6. InviteMemberAsync - Email already invited
        // =============================
        [Test]
        public void InviteMemberAsync_WhenEmailAlreadyInvited_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team", HackathonId = 1 };
            var invitedUser = new User { UserId = 2, Email = "test@email.com" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(2);
            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>()))
                .ReturnsAsync(invitedUser);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(false);
            _teamInvitationRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync(true); // Already invited

            Func<Task> act = async () => await _service.InviteMemberAsync(1, "test@email.com", 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("This email has already been invited.");
        }

        // =============================
        // 7. InviteMemberAsync - Successful invitation
        // =============================
        [Test]
        public async Task InviteMemberAsync_WhenValidRequest_ShouldCreateInvitationSuccessfully()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team", HackathonId = 1 };
            var invitedUser = new User { UserId = 2, Email = "test@email.com" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(2);
            _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>()))
                .ReturnsAsync(invitedUser);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(false);
            _teamInvitationRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync(false);
            _teamInvitationRepo.Setup(r => r.AddAsync(It.IsAny<TeamInvitation>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto());
            _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _service.InviteMemberAsync(1, "test@email.com", 1);

            result.Should().NotBeNull();
            result.Should().Contain("https://sealfall25.somee.com/api/TeamInvitation/accept-link?code=");

            _teamInvitationRepo.Verify(r => r.AddAsync(It.IsAny<TeamInvitation>()), Times.Once);
            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()), Times.Once);
            _emailServiceMock.Verify(e => e.SendEmailAsync("test@email.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // =============================
        // 8. AcceptInvitationAsync - Invalid invitation
        // =============================
        [Test]
        public async Task AcceptInvitationAsync_WhenInvitationInvalid_ReturnsFailedResult()
        {
            var code = Guid.NewGuid();

            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync((TeamInvitation)null);

            var result = await _service.AcceptInvitationAsync(code, 1);

            result.Status.Should().Be("Failed");
            result.Message.Should().Be("Invitation is invalid or expired.");
        }

        // =============================
        // 9. AcceptInvitationAsync - User not found
        // =============================
        [Test]
        public async Task AcceptInvitationAsync_WhenUserNotFound_ReturnsFailedResult()
        {
            var code = Guid.NewGuid();
            var invitation = new TeamInvitation 
            { 
                InvitationCode = code, 
                Status = InvitationStatus.Pending, 
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                InvitedEmail = "test@email.com"
            };

            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync(invitation);
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User)null);

            var result = await _service.AcceptInvitationAsync(code, 99);

            result.Status.Should().Be("Failed");
            result.Message.Should().Be("User not found.");
        }

        // =============================
        // 10. AcceptInvitationAsync - Email mismatch
        // =============================
        [Test]
        public async Task AcceptInvitationAsync_WhenEmailMismatch_ReturnsFailedResult()
        {
            var code = Guid.NewGuid();
            var invitation = new TeamInvitation 
            { 
                InvitationCode = code, 
                Status = InvitationStatus.Pending, 
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                InvitedEmail = "test@email.com"
            };
            var user = new User { UserId = 1, Email = "different@email.com" };

            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync(invitation);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var result = await _service.AcceptInvitationAsync(code, 1);

            result.Status.Should().Be("Failed");
            result.Message.Should().Be("This invitation is not for your account.");
        }

        // =============================
        // 11. AcceptInvitationAsync - Team full
        // =============================
        [Test]
        public async Task AcceptInvitationAsync_WhenTeamFull_ReturnsFailedResult()
        {
            var code = Guid.NewGuid();
            var invitation = new TeamInvitation 
            { 
                InvitationCode = code, 
                Status = InvitationStatus.Pending, 
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                InvitedEmail = "test@email.com",
                TeamId = 1
            };
            var user = new User { UserId = 1, Email = "test@email.com" };
            var team = new Team { TeamId = 1, TeamName = "Test Team", HackathonId = 1 };

            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync(invitation);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(5); // Team is full

            var result = await _service.AcceptInvitationAsync(code, 1);

            result.Status.Should().Be("Failed");
            result.Message.Should().Be("Team already has maximum number of members (5).");
            result.TeamId.Should().Be(1);
            result.TeamName.Should().Be("Test Team");
        }

        // =============================
        // 12. AcceptInvitationAsync - Successful acceptance
        // =============================
        [Test]
        public async Task AcceptInvitationAsync_WhenValidRequest_ShouldAcceptSuccessfully()
        {
            var code = Guid.NewGuid();
            var invitation = new TeamInvitation 
            { 
                InvitationCode = code, 
                Status = InvitationStatus.Pending, 
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                InvitedEmail = "test@email.com",
                TeamId = 1
            };
            var user = new User { UserId = 1, Email = "test@email.com" };
            var team = new Team { TeamId = 1, TeamName = "Test Team", HackathonId = 1 };

            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync(invitation);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(2);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(false);
            _teamInvitationRepo.Setup(r => r.Update(It.IsAny<TeamInvitation>()));
            _teamMemberRepo.Setup(r => r.AddAsync(It.IsAny<TeamMember>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.AcceptInvitationAsync(code, 1);

            result.Status.Should().Be("Success");
            result.Message.Should().Be("You have successfully joined the team.");
            result.TeamId.Should().Be(1);
            result.TeamName.Should().Be("Test Team");

            invitation.Status.Should().Be(InvitationStatus.Accepted);
            _teamMemberRepo.Verify(r => r.AddAsync(It.IsAny<TeamMember>()), Times.Once);
        }

        // =============================
        // 13. RejectInvitationAsync - Successful rejection
        // =============================
        [Test]
        public async Task RejectInvitationAsync_WhenValidRequest_ShouldRejectSuccessfully()
        {
            var code = Guid.NewGuid();
            var invitation = new TeamInvitation 
            { 
                InvitationCode = code, 
                Status = InvitationStatus.Pending, 
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                InvitedEmail = "test@email.com"
            };
            var user = new User { UserId = 1, Email = "test@email.com" };

            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync(invitation);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _teamInvitationRepo.Setup(r => r.Update(It.IsAny<TeamInvitation>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.RejectInvitationAsync(code, 1);

            result.Status.Should().Be("Success");
            result.Message.Should().Be("You have rejected the invitation.");

            invitation.Status.Should().Be(InvitationStatus.Rejected);
            _teamInvitationRepo.Verify(r => r.Update(invitation), Times.Once);
        }

        // =============================
        // 14. GetInvitationStatusAsync - Invitation not found
        // =============================
        [Test]
        public void GetInvitationStatusAsync_WhenInvitationNotFound_Throws()
        {
            var code = Guid.NewGuid();

            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync((TeamInvitation)null);

            Func<Task> act = async () => await _service.GetInvitationStatusAsync(code);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Invitation not found");
        }

        // =============================
        // 15. GetInvitationStatusAsync - Return invitation status
        // =============================
        [Test]
        public async Task GetInvitationStatusAsync_WhenInvitationExists_ShouldReturnStatus()
        {
            var code = Guid.NewGuid();
            var invitation = new TeamInvitation 
            { 
                InvitationCode = code, 
                Status = InvitationStatus.Pending,
                InvitedEmail = "test@email.com"
            };
            var statusDto = new InvitationStatusDto 
            { 
                InvitationCode = code, 
                Status = InvitationStatus.Pending,
                InvitedEmail = "test@email.com"
            };

            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync(invitation);
            _mapperMock.Setup(m => m.Map<InvitationStatusDto>(invitation)).Returns(statusDto);

            var result = await _service.GetInvitationStatusAsync(code);

            result.Should().NotBeNull();
            result.InvitationCode.Should().Be(code);
            result.Status.Should().Be(InvitationStatus.Pending);
            result.InvitedEmail.Should().Be("test@email.com");
        }

        // =============================
        // 16. GetTeamInvitationsByTeamIdAsync - Team not found
        // =============================
        [Test]
        public void GetTeamInvitationsByTeamIdAsync_WhenTeamNotFound_Throws()
        {
            _teamRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Team)null);

            Func<Task> act = async () => await _service.GetTeamInvitationsByTeamIdAsync(99);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team not found.");
        }

        // =============================
        // 17. GetTeamInvitationsByTeamIdAsync - Return team invitations
        // =============================
        [Test]
        public async Task GetTeamInvitationsByTeamIdAsync_WhenTeamExists_ShouldReturnInvitations()
        {
            var team = new Team { TeamId = 1, TeamName = "Test Team" };

            var invitations = new List<TeamInvitation>
    {
        new TeamInvitation { TeamId = 1, InvitedEmail = "user1@email.com", Status = InvitationStatus.Pending },
        new TeamInvitation { TeamId = 1, InvitedEmail = "user2@email.com", Status = InvitationStatus.Accepted }
    };

            var invitationDtos = new List<InvitationStatusDto>
    {
        new InvitationStatusDto { TeamId = 1, InvitedEmail = "user1@email.com", Status = InvitationStatus.Pending },
        new InvitationStatusDto { TeamId = 1, InvitedEmail = "user2@email.com", Status = InvitationStatus.Accepted }
    };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _teamInvitationRepo.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<TeamInvitation, bool>>>(),
                    It.IsAny<Func<IQueryable<TeamInvitation>, IOrderedQueryable<TeamInvitation>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(invitations);

            _mapperMock.Setup(m => m.Map<List<InvitationStatusDto>>(invitations))
                       .Returns(invitationDtos);

            var result = await _service.GetTeamInvitationsByTeamIdAsync(1);

            result.Should().HaveCount(2);
            result[0].InvitedEmail.Should().Be("user1@email.com");
            result[1].InvitedEmail.Should().Be("user2@email.com");
        }

    }
}