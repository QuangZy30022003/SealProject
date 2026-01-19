using AutoMapper;
using Common.DTOs.TeamDto;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Servicefolder;
using System.Linq.Expressions;

namespace NUniTest.Service
{
    [TestFixture]
    public class TeamServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private TeamService _service;

        private Mock<IRepository<Team>> _teamRepo;
        private Mock<IRepository<Chapter>> _chapterRepo;
        private Mock<IRepository<StudentVerification>> _verifyRepo;
        private Mock<IRepository<TeamMember>> _teamMemberRepo;
        private Mock<IRepository<HackathonPhase>> _phaseRepo;
        private Mock<IRepository<FinalQualification>> _finalRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _teamRepo = new Mock<IRepository<Team>>();
            _chapterRepo = new Mock<IRepository<Chapter>>();
            _verifyRepo = new Mock<IRepository<StudentVerification>>();
            _teamMemberRepo = new Mock<IRepository<TeamMember>>();
            _phaseRepo = new Mock<IRepository<HackathonPhase>>();
            _finalRepo = new Mock<IRepository<FinalQualification>>();

            _uowMock.Setup(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.Setup(u => u.Chapters).Returns(_chapterRepo.Object);
            _uowMock.Setup(u => u.StudentVerifications).Returns(_verifyRepo.Object);
            _uowMock.Setup(u => u.TeamMembers).Returns(_teamMemberRepo.Object);
            _uowMock.Setup(u => u.HackathonPhases).Returns(_phaseRepo.Object);
            _uowMock.Setup(u => u.FinalQualifications).Returns(_finalRepo.Object);

            _service = new TeamService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // 1. Chapter không tồn tại
        // =============================
        [Test]
        public void CreateTeamAsync_WhenChapterNotExist_Throws()
        {
            var dto = new CreateTeamDto { ChapterId = 10, TeamName = "Alpha" };

            _chapterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Chapter, bool>>>()))
                        .ReturnsAsync(false);

            Func<Task> act = async () => await _service.CreateTeamAsync(dto, 1);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Chapter does not exist. Please create a chapter first.");
        }

        // =============================
        // 2. Trùng tên team
        // =============================
        [Test]
        public void CreateTeamAsync_WhenDuplicateName_Throws()
        {
            var dto = new CreateTeamDto { ChapterId = 1, TeamName = "Alpha" };

            _chapterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Chapter, bool>>>()))
                        .ReturnsAsync(true);

            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                     .ReturnsAsync(true);

            Func<Task> act = async () => await _service.CreateTeamAsync(dto, 1);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Team name already exists in this chapter.");
        }

        // =============================
        // 3. User chưa verify
        // =============================
        [Test]
        public void CreateTeamAsync_WhenUserNotApproved_Throws()
        {
            var dto = new CreateTeamDto { ChapterId = 1, TeamName = "Alpha" };

            _chapterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Chapter, bool>>>()))
                        .ReturnsAsync(true);

            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                     .ReturnsAsync(false);

            _verifyRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                       .ReturnsAsync(new StudentVerification { Status = "Pending" });

            Func<Task> act = async () => await _service.CreateTeamAsync(dto, 1);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Student has not been approved for verification. Cannot create team.");
        }

        // =============================
        // 4. Tạo team thành công
        // =============================
        [Test]
        public async Task CreateTeamAsync_WhenValid_ShouldCreate()
        {
            var dto = new CreateTeamDto { ChapterId = 1, TeamName = "Alpha" };
            var entity = new Team { TeamId = 99, TeamName = "Alpha", ChapterId = 1 };

            _chapterRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Chapter, bool>>>()))
                        .ReturnsAsync(true);

            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                     .ReturnsAsync(false);

            _verifyRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                       .ReturnsAsync(new StudentVerification { Status = "Approved" });

            _mapperMock.Setup(m => m.Map<Team>(dto)).Returns(entity);

            _teamRepo.Setup(r => r.AddAsync(It.IsAny<Team>())).Returns(Task.CompletedTask);
            _teamMemberRepo.Setup(r => r.AddAsync(It.IsAny<TeamMember>())).Returns(Task.CompletedTask);

            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _uowMock.Setup(u => u.Teams.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<Team, bool>>>(),
                It.IsAny<Expression<Func<Team, object>>>(),
                It.IsAny<Expression<Func<Team, object>>>(),
                It.IsAny<Expression<Func<Team, object>>>()))
                .ReturnsAsync(entity);

            _mapperMock.Setup(m => m.Map<TeamDto>(entity))
                       .Returns(new TeamDto { TeamId = 99, TeamName = "Alpha" });

            var result = await _service.CreateTeamAsync(dto, 5);

            result.TeamId.Should().Be(99);

            _teamRepo.Verify(r => r.AddAsync(It.IsAny<Team>()), Times.Once);
            _teamMemberRepo.Verify(r => r.AddAsync(It.IsAny<TeamMember>()), Times.Once);

            _uowMock.Verify(u => u.SaveAsync(null), Times.Exactly(2));
        }

     
       

        // =============================
        // 7. Không phải leader → lỗi
        // =============================
        [Test]
        public void DeleteAsync_WhenNotLeader_Throws()
        {
            var team = new Team { TeamId = 7, TeamLeaderId = 10 };

            _teamRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.DeleteAsync(7, 5);

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not the leader of this team.");
        }
    }
}
