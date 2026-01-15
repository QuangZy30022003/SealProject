using AutoMapper;
using Common.DTOs.NotificationDto;
using Common.DTOs.TeamJoinRequestDto;
using Common.Enums;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Interface;
using Service.Servicefolder;

namespace NUniTest.Service
{
    [TestFixture]
    public class TeamJoinRequestServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<INotificationService> _notificationServiceMock;
        private TeamJoinRequestService _service;

        private Mock<IRepository<Team>> _teamRepo;
        private Mock<IRepository<User>> _userRepo;
        private Mock<IRepository<TeamMember>> _teamMemberRepo;
        private Mock<IRepository<TeamInvitation>> _teamInvitationRepo;
        private Mock<IRepository<TeamJoinRequest>> _teamJoinRequestRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _notificationServiceMock = new Mock<INotificationService>();

            _teamRepo = new Mock<IRepository<Team>>();
            _userRepo = new Mock<IRepository<User>>();
            _teamMemberRepo = new Mock<IRepository<TeamMember>>();
            _teamInvitationRepo = new Mock<IRepository<TeamInvitation>>();
            _teamJoinRequestRepo = new Mock<IRepository<TeamJoinRequest>>();

            _uowMock.Setup(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.Setup(u => u.Users).Returns(_userRepo.Object);
            _uowMock.Setup(u => u.TeamMembers).Returns(_teamMemberRepo.Object);
            _uowMock.Setup(u => u.TeamInvitations).Returns(_teamInvitationRepo.Object);
            _uowMock.Setup(u => u.TeamJoinRequests).Returns(_teamJoinRequestRepo.Object);

            _service = new TeamJoinRequestService(_uowMock.Object, _mapperMock.Object, _notificationServiceMock.Object);
        }

        // =============================
        // 1. CreateJoinRequestAsync - Team not found
        // =============================
        [Test]
        public void CreateJoinRequestAsync_WhenTeamNotFound_Throws()
        {
            var dto = new CreateJoinRequestDto { TeamId = 99, Message = "Please let me join" };

            _teamRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Team)null);

            Func<Task> act = async () => await _service.CreateJoinRequestAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team not found.");
        }

        // =============================
        // 2. CreateJoinRequestAsync - User not found
        // =============================
        [Test]
        public void CreateJoinRequestAsync_WhenUserNotFound_Throws()
        {
            var dto = new CreateJoinRequestDto { TeamId = 1, Message = "Please let me join" };
            var team = new Team { TeamId = 1, TeamName = "Test Team" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User)null);

            Func<Task> act = async () => await _service.CreateJoinRequestAsync(dto, 99);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("User not found.");
        }

        // =============================
        // 3. CreateJoinRequestAsync - User already in another unregistered team
        // =============================
        [Test]
        public void CreateJoinRequestAsync_WhenUserInAnotherUnregisteredTeam_Throws()
        {
            var dto = new CreateJoinRequestDto { TeamId = 1, Message = "Please let me join" };
            var team = new Team { TeamId = 1, TeamName = "Test Team", HackathonId = null };
            var user = new User { UserId = 1, Email = "test@email.com" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(true); // User is in another unregistered team

            Func<Task> act = async () => await _service.CreateJoinRequestAsync(dto, 1);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You are already in another team that hasn't registered for any hackathon.");
        }

        // =============================
        // 4. CreateJoinRequestAsync - User already in another team same hackathon
        // =============================
        [Test]
        public void CreateJoinRequestAsync_WhenUserInAnotherTeamSameHackathon_Throws()
        {
            var dto = new CreateJoinRequestDto { TeamId = 1, Message = "Please let me join" };
            var team = new Team { TeamId = 1, TeamName = "Test Team", HackathonId = 1 };
            var user = new User { UserId = 1, Email = "test@email.com" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(true); // User is in another team in same hackathon

            Func<Task> act = async () => await _service.CreateJoinRequestAsync(dto, 1);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You are already a member of another team in this hackathon.");
        }

        // =============================
        // 5. CreateJoinRequestAsync - User has pending invitation
        // =============================
        [Test]
        public void CreateJoinRequestAsync_WhenUserHasPendingInvitation_Throws()
        {
            var dto = new CreateJoinRequestDto { TeamId = 1, Message = "Please let me join" };
            var team = new Team { TeamId = 1, TeamName = "Test Team", HackathonId = 1 };
            var user = new User { UserId = 1, Email = "test@email.com" };
            var pendingInvite = new TeamInvitation { InvitedEmail = "test@email.com", Status = "Pending" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(false);
            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync(pendingInvite);

            Func<Task> act = async () => await _service.CreateJoinRequestAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You already have a pending invitation.");
        }

        // =============================
        // 6. CreateJoinRequestAsync - User already has pending request
        // =============================
        [Test]
        public void CreateJoinRequestAsync_WhenUserHasPendingRequest_Throws()
        {
            var dto = new CreateJoinRequestDto { TeamId = 1, Message = "Please let me join" };
            var team = new Team { TeamId = 1, TeamName = "Test Team", HackathonId = 1 };
            var user = new User { UserId = 1, Email = "test@email.com" };
            var existingRequest = new TeamJoinRequest { TeamId = 1, UserId = 1, Status = JoinRequestStatus.Pending };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(false);
            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync((TeamInvitation)null);
            _teamJoinRequestRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, bool>>>()))
                .ReturnsAsync(existingRequest);

            Func<Task> act = async () => await _service.CreateJoinRequestAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You already have a pending request for this team.");
        }

        // =============================
        // 7. CreateJoinRequestAsync - Team is full
        // =============================
        [Test]
        public void CreateJoinRequestAsync_WhenTeamIsFull_Throws()
        {
            var dto = new CreateJoinRequestDto { TeamId = 1, Message = "Please let me join" };
            var team = new Team { TeamId = 1, TeamName = "Test Team", HackathonId = 1 };
            var user = new User { UserId = 1, Email = "test@email.com" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(false);
            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync((TeamInvitation)null);
            _teamJoinRequestRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, bool>>>()))
                .ReturnsAsync((TeamJoinRequest)null);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(5); // Team is full

            Func<Task> act = async () => await _service.CreateJoinRequestAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("This team is full.");
        }

        // =============================
        // 8. CreateJoinRequestAsync - Successful request creation
        // =============================
        [Test]
        public async Task CreateJoinRequestAsync_WhenValidRequest_ShouldCreateSuccessfully()
        {
            var dto = new CreateJoinRequestDto { TeamId = 1, Message = "Please let me join" };
            var team = new Team { TeamId = 1, TeamName = "Test Team", HackathonId = 1 };
            var user = new User { UserId = 1, Email = "test@email.com" };
            var request = new TeamJoinRequest { RequestId = 1, TeamId = 1, UserId = 1, Message = "Please let me join", Status = JoinRequestStatus.Pending };
            var responseDto = new JoinRequestResponseDto { RequestId = 1, TeamId = 1, UserId = 1, Message = "Please let me join", Status = JoinRequestStatus.Pending };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync(false);
            _teamInvitationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamInvitation, bool>>>()))
                .ReturnsAsync((TeamInvitation)null);
            _teamJoinRequestRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, bool>>>()))
                .ReturnsAsync((TeamJoinRequest)null);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(2);
            _teamJoinRequestRepo.Setup(r => r.AddAsync(It.IsAny<TeamJoinRequest>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<JoinRequestResponseDto>(It.IsAny<TeamJoinRequest>())).Returns(responseDto);

            var result = await _service.CreateJoinRequestAsync(dto, 1);

            result.Should().NotBeNull();
            result.Message.Should().Be("Please let me join");
            result.Status.Should().Be(JoinRequestStatus.Pending);

            _teamJoinRequestRepo.Verify(r => r.AddAsync(It.IsAny<TeamJoinRequest>()), Times.Once);
        }

        // =============================
        // 9. GetJoinRequestsForTeamAsync - Return team join requests
        // =============================
        [Test]
        public async Task GetJoinRequestsForTeamAsync_ShouldReturnTeamRequests()
        {
            var requests = new List<TeamJoinRequest>
            {
                new TeamJoinRequest { RequestId = 1, TeamId = 1, UserId = 1, Status = JoinRequestStatus.Pending },
                new TeamJoinRequest { RequestId = 2, TeamId = 1, UserId = 2, Status = JoinRequestStatus.Approved }
            };
            var responseDtos = new List<JoinRequestResponseDto>
            {
                new JoinRequestResponseDto { RequestId = 1, TeamId = 1, UserId = 1, Status = JoinRequestStatus.Pending },
                new JoinRequestResponseDto { RequestId = 2, TeamId = 1, UserId = 2, Status = JoinRequestStatus.Approved }
            };

            _teamJoinRequestRepo.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, bool>>>(),
                It.IsAny<Func<IQueryable<TeamJoinRequest>, IOrderedQueryable<TeamJoinRequest>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(requests);
            _mapperMock.Setup(m => m.Map<IEnumerable<JoinRequestResponseDto>>(requests)).Returns(responseDtos);

            var result = await _service.GetJoinRequestsForTeamAsync(1);

            result.Should().HaveCount(2);
            result.First().Status.Should().Be(JoinRequestStatus.Pending);
            result.Last().Status.Should().Be(JoinRequestStatus.Approved);
        }

        // =============================
        // 10. GetJoinRequestsByUserAsync - Return user join requests
        // =============================
        [Test]
        public async Task GetJoinRequestsByUserAsync_ShouldReturnUserRequests()
        {
            var requests = new List<TeamJoinRequest>
            {
                new TeamJoinRequest { RequestId = 1, TeamId = 1, UserId = 1, Status = JoinRequestStatus.Pending },
                new TeamJoinRequest { RequestId = 2, TeamId = 2, UserId = 1, Status = JoinRequestStatus.Rejected }
            };
            var responseDtos = new List<JoinRequestResponseDto>
            {
                new JoinRequestResponseDto { RequestId = 1, TeamId = 1, UserId = 1, Status = JoinRequestStatus.Pending },
                new JoinRequestResponseDto { RequestId = 2, TeamId = 2, UserId = 1, Status = JoinRequestStatus.Rejected }
            };

            _teamJoinRequestRepo.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, bool>>>(),
                It.IsAny<Func<IQueryable<TeamJoinRequest>, IOrderedQueryable<TeamJoinRequest>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(requests);
            _mapperMock.Setup(m => m.Map<IEnumerable<JoinRequestResponseDto>>(requests)).Returns(responseDtos);

            var result = await _service.GetJoinRequestsByUserAsync(1);

            result.Should().HaveCount(2);
            result.First().TeamId.Should().Be(1);
            result.Last().TeamId.Should().Be(2);
        }

        // =============================
        // 11. RespondToJoinRequestAsync - Request not found
        // =============================
        [Test]
        public async Task RespondToJoinRequestAsync_WhenRequestNotFound_ReturnsNull()
        {
            var dto = new RespondToJoinRequestDto { Status = JoinRequestStatus.Approved, LeaderResponse = "Welcome!" };

            _teamJoinRequestRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((TeamJoinRequest)null);

            var result = await _service.RespondToJoinRequestAsync(99, dto, 1);

            result.Should().BeNull();
        }

        // =============================
        // 12. RespondToJoinRequestAsync - Team not found
        // =============================
        [Test]
        public void RespondToJoinRequestAsync_WhenTeamNotFound_Throws()
        {
            var dto = new RespondToJoinRequestDto { Status = JoinRequestStatus.Approved, LeaderResponse = "Welcome!" };
            var request = new TeamJoinRequest { RequestId = 1, TeamId = 99, UserId = 1, Status = JoinRequestStatus.Pending };

            _teamJoinRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _teamRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Team)null);

            Func<Task> act = async () => await _service.RespondToJoinRequestAsync(1, dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team not found.");
        }

        // =============================
        // 13. RespondToJoinRequestAsync - User not team leader
        // =============================
        [Test]
        public void RespondToJoinRequestAsync_WhenUserNotLeader_Throws()
        {
            var dto = new RespondToJoinRequestDto { Status = JoinRequestStatus.Approved, LeaderResponse = "Welcome!" };
            var request = new TeamJoinRequest { RequestId = 1, TeamId = 1, UserId = 2, Status = JoinRequestStatus.Pending };
            var team = new Team { TeamId = 1, TeamLeaderId = 2, TeamName = "Test Team" };

            _teamJoinRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.RespondToJoinRequestAsync(1, dto, 1); // User 1 is not leader

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only the team leader can respond to join requests.");
        }

        // =============================
        // 14. RespondToJoinRequestAsync - Request already processed
        // =============================
        [Test]
        public void RespondToJoinRequestAsync_WhenRequestAlreadyProcessed_Throws()
        {
            var dto = new RespondToJoinRequestDto { Status = JoinRequestStatus.Approved, LeaderResponse = "Welcome!" };
            var request = new TeamJoinRequest { RequestId = 1, TeamId = 1, UserId = 2, Status = JoinRequestStatus.Approved }; // Already processed
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };

            _teamJoinRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.RespondToJoinRequestAsync(1, dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("This request has already been processed.");
        }

        // =============================
        // 15. RespondToJoinRequestAsync - Auto reject when team is full
        // =============================
        [Test]
        public async Task RespondToJoinRequestAsync_WhenTeamFull_ShouldAutoReject()
        {
            var dto = new RespondToJoinRequestDto { Status = JoinRequestStatus.Approved, LeaderResponse = "Welcome!" };
            var request = new TeamJoinRequest { RequestId = 1, TeamId = 1, UserId = 2, Status = JoinRequestStatus.Pending };
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };
            var updatedRequest = new TeamJoinRequest 
            { 
                RequestId = 1, 
                TeamId = 1, 
                UserId = 2, 
                Status = JoinRequestStatus.Rejected,
                LeaderResponse = "Team is full (maximum 5 members)."
            };
            var responseDto = new JoinRequestResponseDto 
            { 
                RequestId = 1, 
                Status = JoinRequestStatus.Rejected,
                LeaderResponse = "Team is full (maximum 5 members)."
            };

            _teamJoinRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(5); // Team is full
            _teamJoinRequestRepo.Setup(r => r.Update(It.IsAny<TeamJoinRequest>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _teamJoinRequestRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, object>>[]>()))
                .ReturnsAsync(updatedRequest);
            _mapperMock.Setup(m => m.Map<JoinRequestResponseDto>(updatedRequest)).Returns(responseDto);

            var result = await _service.RespondToJoinRequestAsync(1, dto, 1);

            result.Should().NotBeNull();
            result.Status.Should().Be(JoinRequestStatus.Rejected);
            result.LeaderResponse.Should().Be("Team is full (maximum 5 members).");

            request.Status.Should().Be(JoinRequestStatus.Rejected);
            request.LeaderResponse.Should().Be("Team is full (maximum 5 members).");
        }

        // =============================
        // 16. RespondToJoinRequestAsync - Successful approval
        // =============================
        [Test]
        public async Task RespondToJoinRequestAsync_WhenApproved_ShouldAddMemberAndNotify()
        {
            var dto = new RespondToJoinRequestDto { Status = JoinRequestStatus.Approved, LeaderResponse = "Welcome!" };
            var request = new TeamJoinRequest { RequestId = 1, TeamId = 1, UserId = 2, Status = JoinRequestStatus.Pending };
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };
            var updatedRequest = new TeamJoinRequest 
            { 
                RequestId = 1, 
                TeamId = 1, 
                UserId = 2, 
                Status = JoinRequestStatus.Approved,
                LeaderResponse = "Welcome!"
            };
            var responseDto = new JoinRequestResponseDto 
            { 
                RequestId = 1, 
                Status = JoinRequestStatus.Approved,
                LeaderResponse = "Welcome!"
            };

            _teamJoinRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(2);
            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync((TeamMember)null);
            _teamRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync((Team)null);
            _teamMemberRepo.Setup(r => r.AddAsync(It.IsAny<TeamMember>())).Returns(Task.CompletedTask);
            _teamJoinRequestRepo.Setup(r => r.Update(It.IsAny<TeamJoinRequest>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto());
            _teamJoinRequestRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, object>>[]>()))
                .ReturnsAsync(updatedRequest);
            _mapperMock.Setup(m => m.Map<JoinRequestResponseDto>(updatedRequest)).Returns(responseDto);

            var result = await _service.RespondToJoinRequestAsync(1, dto, 1);

            result.Should().NotBeNull();
            result.Status.Should().Be(JoinRequestStatus.Approved);
            result.LeaderResponse.Should().Be("Welcome!");

            request.Status.Should().Be(JoinRequestStatus.Approved);
            request.LeaderResponse.Should().Be("Welcome!");

            _teamMemberRepo.Verify(r => r.AddAsync(It.IsAny<TeamMember>()), Times.Once);
            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()), Times.Once);
        }

        // =============================
        // 17. RespondToJoinRequestAsync - Successful rejection
        // =============================
        [Test]
        public async Task RespondToJoinRequestAsync_WhenRejected_ShouldNotifyUser()
        {
            var dto = new RespondToJoinRequestDto { Status = JoinRequestStatus.Rejected, LeaderResponse = "Not a good fit" };
            var request = new TeamJoinRequest { RequestId = 1, TeamId = 1, UserId = 2, Status = JoinRequestStatus.Pending };
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };
            var updatedRequest = new TeamJoinRequest 
            { 
                RequestId = 1, 
                TeamId = 1, 
                UserId = 2, 
                Status = JoinRequestStatus.Rejected,
                LeaderResponse = "Not a good fit"
            };
            var responseDto = new JoinRequestResponseDto 
            { 
                RequestId = 1, 
                Status = JoinRequestStatus.Rejected,
                LeaderResponse = "Not a good fit"
            };

            _teamJoinRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(2);
            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync((TeamMember)null);
            _teamRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Team, bool>>>()))
                .ReturnsAsync((Team)null);
            _teamJoinRequestRepo.Setup(r => r.Update(It.IsAny<TeamJoinRequest>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto());
            _teamJoinRequestRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, object>>[]>()))
                .ReturnsAsync(updatedRequest);
            _mapperMock.Setup(m => m.Map<JoinRequestResponseDto>(updatedRequest)).Returns(responseDto);

            var result = await _service.RespondToJoinRequestAsync(1, dto, 1);

            result.Should().NotBeNull();
            result.Status.Should().Be(JoinRequestStatus.Rejected);
            result.LeaderResponse.Should().Be("Not a good fit");

            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()), Times.Once);
        }

        // =============================
        // 18. GetJoinRequestByIdAsync - Request exists
        // =============================
        [Test]
        public async Task GetJoinRequestByIdAsync_WhenRequestExists_ShouldReturnRequest()
        {
            var request = new TeamJoinRequest { RequestId = 1, TeamId = 1, UserId = 1, Status = JoinRequestStatus.Pending };
            var responseDto = new JoinRequestResponseDto { RequestId = 1, TeamId = 1, UserId = 1, Status = JoinRequestStatus.Pending };

            _teamJoinRequestRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, object>>[]>()))
                .ReturnsAsync(request);
            _mapperMock.Setup(m => m.Map<JoinRequestResponseDto>(request)).Returns(responseDto);

            var result = await _service.GetJoinRequestByIdAsync(1);

            result.Should().NotBeNull();
            result.RequestId.Should().Be(1);
            result.Status.Should().Be(JoinRequestStatus.Pending);
        }

        // =============================
        // 19. GetJoinRequestByIdAsync - Request not exists
        // =============================
        [Test]
        public async Task GetJoinRequestByIdAsync_WhenRequestNotExists_ShouldReturnNull()
        {
            _teamJoinRequestRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamJoinRequest, object>>[]>()))
                .ReturnsAsync((TeamJoinRequest)null);

            var result = await _service.GetJoinRequestByIdAsync(99);

            result.Should().BeNull();
        }
    }
}