using AutoMapper;
using Common.DTOs.TeamMemberDto;
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
    public class TeamMemberServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<INotificationService> _notificationService;
        private TeamMemberService _service;

        private Mock<IRepository<Team>> _teamRepo;
        private Mock<IRepository<TeamMember>> _teamMemberRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _notificationService = new Mock<INotificationService>();

            _teamRepo = new Mock<IRepository<Team>>();
            _teamMemberRepo = new Mock<IRepository<TeamMember>>();

            _uowMock.Setup(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.Setup(u => u.TeamMembers).Returns(_teamMemberRepo.Object);

            _service = new TeamMemberService(_uowMock.Object, _mapperMock.Object, _notificationService.Object);
        }

        // =============================
        // 1. KickMemberAsync - Team not found
        // =============================
        [Test]
        public void KickMemberAsync_WhenTeamNotFound_Throws()
        {
            _teamRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Team)null);

            Func<Task> act = async () => await _service.KickMemberAsync(99, 2, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team not found.");
        }

        // =============================
        // 2. KickMemberAsync - User not team leader
        // =============================
        [Test]
        public void KickMemberAsync_WhenUserNotLeader_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 2, TeamName = "Test Team" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.KickMemberAsync(1, 3, 1); // User 1 is not leader

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only leader can kick members.");
        }

        // =============================
        // 3. KickMemberAsync - Member not found in team
        // =============================
        [Test]
        public void KickMemberAsync_WhenMemberNotFound_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync((TeamMember)null);

            Func<Task> act = async () => await _service.KickMemberAsync(1, 99, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Member not found in team.");
        }

        // =============================
        // 4. KickMemberAsync - Cannot kick leader
        // =============================
        [Test]
        public void KickMemberAsync_WhenTryingToKickLeader_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };
            var leaderMember = new TeamMember { TeamId = 1, UserId = 1, RoleInTeam = "Leader" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(leaderMember);

            Func<Task> act = async () => await _service.KickMemberAsync(1, 1, 1); // Trying to kick self (leader)

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Leader cannot be kicked.");
        }

        // =============================
        // 5. KickMemberAsync - Successful kick
        // =============================
        [Test]
        public async Task KickMemberAsync_WhenValidRequest_ShouldKickSuccessfully()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };
            var member = new TeamMember { TeamId = 1, UserId = 2, RoleInTeam = "Member" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(member);
            _teamMemberRepo.Setup(r => r.Remove(It.IsAny<TeamMember>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.KickMemberAsync(1, 2, 1);

            result.Should().Be("Member has been kicked successfully.");
            _teamMemberRepo.Verify(r => r.Remove(member), Times.Once);
        }

        // =============================
        // 6. LeaveTeamAsync - Member not in team
        // =============================
        [Test]
        public void LeaveTeamAsync_WhenMemberNotInTeam_Throws()
        {
            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync((TeamMember)null);

            Func<Task> act = async () => await _service.LeaveTeamAsync(1, 99);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not in this team.");
        }

        // =============================
        // 7. LeaveTeamAsync - Leader cannot leave
        // =============================
        [Test]
        public void LeaveTeamAsync_WhenLeaderTriesToLeave_Throws()
        {
            var member = new TeamMember { TeamId = 1, UserId = 1, RoleInTeam = "Leader" };
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(member);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.LeaveTeamAsync(1, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Leader cannot leave the team. Please transfer leadership first.");
        }

        // =============================
        // 8. LeaveTeamAsync - Successful leave
        // =============================
        [Test]
        public async Task LeaveTeamAsync_WhenValidRequest_ShouldLeaveSuccessfully()
        {
            var member = new TeamMember { TeamId = 1, UserId = 2, RoleInTeam = "Member" };
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(member);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.Remove(It.IsAny<TeamMember>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.LeaveTeamAsync(1, 2);

            result.Should().Be("You have left the team.");
            _teamMemberRepo.Verify(r => r.Remove(member), Times.Once);
        }

        // =============================
        // 9. ChangeLeaderAsync - Team not found
        // =============================
        [Test]
        public void ChangeLeaderAsync_WhenTeamNotFound_Throws()
        {
            _teamRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Team)null);

            Func<Task> act = async () => await _service.ChangeLeaderAsync(99, 2, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team not found.");
        }

        // =============================
        // 10. ChangeLeaderAsync - User not current leader
        // =============================
        [Test]
        public void ChangeLeaderAsync_WhenUserNotCurrentLeader_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 2, TeamName = "Test Team" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.ChangeLeaderAsync(1, 3, 1); // User 1 is not current leader

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only the current leader can transfer leadership.");
        }

        // =============================
        // 11. ChangeLeaderAsync - Cannot transfer to self
        // =============================
        [Test]
        public void ChangeLeaderAsync_WhenTransferringToSelf_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.ChangeLeaderAsync(1, 1, 1); // Same user

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Cannot transfer leadership to yourself.");
        }

        // =============================
        // 12. ChangeLeaderAsync - New leader not in team
        // =============================
        [Test]
        public void ChangeLeaderAsync_WhenNewLeaderNotInTeam_Throws()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync((TeamMember)null);

            Func<Task> act = async () => await _service.ChangeLeaderAsync(1, 99, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("The specified user is not a member of this team.");
        }

        // =============================
        // 13. ChangeLeaderAsync - Successful leadership transfer
        // =============================
        [Test]
        public async Task ChangeLeaderAsync_WhenValidRequest_ShouldTransferSuccessfully()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };
            var oldLeader = new TeamMember { TeamId = 1, UserId = 1, RoleInTeam = "Leader" };
            var newLeader = new TeamMember { TeamId = 1, UserId = 2, RoleInTeam = "Member" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(newLeader) // First call for new leader check
                .ReturnsAsync(oldLeader); // Second call for old leader
            _teamMemberRepo.Setup(r => r.Update(It.IsAny<TeamMember>()));
            _teamRepo.Setup(r => r.Update(It.IsAny<Team>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.ChangeLeaderAsync(1, 2, 1);

            result.Should().Be("Leadership has been successfully transferred to user ID 2.");
            oldLeader.RoleInTeam.Should().Be("Member");
            newLeader.RoleInTeam.Should().Be("Leader");
            team.TeamLeaderId.Should().Be(2);

            _teamMemberRepo.Verify(r => r.Update(oldLeader), Times.Once);
            _teamMemberRepo.Verify(r => r.Update(newLeader), Times.Once);
            _teamRepo.Verify(r => r.Update(team), Times.Once);
        }

        // =============================
        // 14. GetTeamMembersAsync - Return team members
        // =============================
        [Test]
        public async Task GetTeamMembersAsync_ShouldReturnTeamMembers()
        {
            var members = new List<TeamMember>
            {
                new TeamMember 
                { 
                    TeamId = 1, 
                    UserId = 1, 
                    RoleInTeam = "Leader",
                    User = new User { UserId = 1, FullName = "Leader Name" }
                },
                new TeamMember 
                { 
                    TeamId = 1, 
                    UserId = 2, 
                    RoleInTeam = "Member",
                    User = new User { UserId = 2, FullName = "Member Name" }
                }
            };
            var memberDtos = new List<TeamMemberDto>
            {
                new TeamMemberDto { TeamId = 1, UserId = 1, RoleInTeam = "Leader", UserName = "Leader Name" },
                new TeamMemberDto { TeamId = 1, UserId = 2, RoleInTeam = "Member", UserName = "Member Name" }
            };

            _teamMemberRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, object>>[]>()))
                .ReturnsAsync(members);
            _mapperMock.Setup(m => m.Map<IEnumerable<TeamMemberDto>>(members)).Returns(memberDtos);

            var result = await _service.GetTeamMembersAsync(1);

            result.Should().HaveCount(2);
            result.First().RoleInTeam.Should().Be("Leader");
            result.Last().RoleInTeam.Should().Be("Member");
        }

        // =============================
        // 15. CheckLeaderAsync - User not in team
        // =============================
        [Test]
        public void CheckLeaderAsync_WhenUserNotInTeam_Throws()
        {
            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync((TeamMember)null);

            Func<Task> act = async () => await _service.CheckLeaderAsync(1, 99);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("User is not in this team.");
        }

        // =============================
        // 16. CheckLeaderAsync - User is leader
        // =============================
        [Test]
        public async Task CheckLeaderAsync_WhenUserIsLeader_ReturnsTrue()
        {
            var member = new TeamMember { TeamId = 1, UserId = 1, RoleInTeam = "Leader" };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(member);

            var result = await _service.CheckLeaderAsync(1, 1);

            result.Should().BeTrue();
        }

        // =============================
        // 17. CheckLeaderAsync - User is TeamLeader (case insensitive)
        // =============================
        [Test]
        public async Task CheckLeaderAsync_WhenUserIsTeamLeader_ReturnsTrue()
        {
            var member = new TeamMember { TeamId = 1, UserId = 1, RoleInTeam = "TeamLeader" };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(member);

            var result = await _service.CheckLeaderAsync(1, 1);

            result.Should().BeTrue();
        }

        // =============================
        // 18. CheckLeaderAsync - User is not leader
        // =============================
        [Test]
        public async Task CheckLeaderAsync_WhenUserIsNotLeader_ReturnsFalse()
        {
            var member = new TeamMember { TeamId = 1, UserId = 1, RoleInTeam = "Member" };

            _teamMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(member);

            var result = await _service.CheckLeaderAsync(1, 1);

            result.Should().BeFalse();
        }

        // =============================
        // 19. ChangeLeaderAsync - No old leader found (edge case)
        // =============================
        [Test]
        public async Task ChangeLeaderAsync_WhenNoOldLeaderFound_ShouldStillTransferSuccessfully()
        {
            var team = new Team { TeamId = 1, TeamLeaderId = 1, TeamName = "Test Team" };
            var newLeader = new TeamMember { TeamId = 1, UserId = 2, RoleInTeam = "Member" };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _teamMemberRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<TeamMember, bool>>>()))
                .ReturnsAsync(newLeader) // First call for new leader check
                .ReturnsAsync((TeamMember)null); // Second call for old leader (not found)
            _teamMemberRepo.Setup(r => r.Update(It.IsAny<TeamMember>()));
            _teamRepo.Setup(r => r.Update(It.IsAny<Team>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.ChangeLeaderAsync(1, 2, 1);

            result.Should().Be("Leadership has been successfully transferred to user ID 2.");
            newLeader.RoleInTeam.Should().Be("Leader");
            team.TeamLeaderId.Should().Be(2);

            _teamMemberRepo.Verify(r => r.Update(newLeader), Times.Once);
            _teamRepo.Verify(r => r.Update(team), Times.Once);
        }
    }
}