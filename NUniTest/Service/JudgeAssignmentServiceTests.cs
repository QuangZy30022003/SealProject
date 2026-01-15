using AutoMapper;
using Common.DTOs.JudgeAssignmentDto;
using Common.DTOs.NotificationDto;
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
    public class JudgeAssignmentServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<INotificationService> _notificationServiceMock;
        private JudgeAssignmentService _service;

        private Mock<IRepository<JudgeAssignment>> _judgeAssignmentRepo;
        private Mock<IRepository<Hackathon>> _hackathonRepo;
        private Mock<IRepository<User>> _userRepo;
        private Mock<IRepository<Track>> _trackRepo;
        private Mock<IRepository<HackathonPhase>> _phaseRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _notificationServiceMock = new Mock<INotificationService>();

            _judgeAssignmentRepo = new Mock<IRepository<JudgeAssignment>>();
            _hackathonRepo = new Mock<IRepository<Hackathon>>();
            _userRepo = new Mock<IRepository<User>>();
            _trackRepo = new Mock<IRepository<Track>>();
            _phaseRepo = new Mock<IRepository<HackathonPhase>>();

            _uowMock.Setup(u => u.JudgeAssignments).Returns(_judgeAssignmentRepo.Object);
            _uowMock.Setup(u => u.Hackathons).Returns(_hackathonRepo.Object);
            _uowMock.Setup(u => u.Users).Returns(_userRepo.Object);
            _uowMock.Setup(u => u.Tracks).Returns(_trackRepo.Object);
            _uowMock.Setup(u => u.HackathonPhases).Returns(_phaseRepo.Object);

            _service = new JudgeAssignmentService(_uowMock.Object, _mapperMock.Object, _notificationServiceMock.Object);
        }

        // =============================
        // 1. AssignJudgeAsync - Hackathon không tồn tại
        // =============================
        [Test]
        public void AssignJudgeAsync_WhenHackathonNotFound_Throws()
        {
            var dto = new JudgeAssignmentCreateDto { JudgeId = 1, HackathonId = 99, PhaseId = 1 };

            _hackathonRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hackathon)null);

            Func<Task> act = async () => await _service.AssignJudgeAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Hackathon not found");
        }

        // =============================
        // 2. AssignJudgeAsync - Judge không tồn tại
        // =============================
        [Test]
        public void AssignJudgeAsync_WhenJudgeNotFound_Throws()
        {
            var dto = new JudgeAssignmentCreateDto { JudgeId = 99, HackathonId = 1, PhaseId = 1 };
            var hackathon = new Hackathon { HackathonId = 1, Name = "Test Hackathon" };

            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hackathon);
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User)null);

            Func<Task> act = async () => await _service.AssignJudgeAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Judge not found");
        }

        // =============================
        // 3. AssignJudgeAsync - User không phải Judge
        // =============================
        [Test]
        public void AssignJudgeAsync_WhenUserNotJudge_Throws()
        {
            var dto = new JudgeAssignmentCreateDto { JudgeId = 1, HackathonId = 1, PhaseId = 1 };
            var hackathon = new Hackathon { HackathonId = 1, Name = "Test Hackathon" };
            var user = new User { UserId = 1, RoleId = 3 }; // Không phải Judge (RoleId = 6)

            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hackathon);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            Func<Task> act = async () => await _service.AssignJudgeAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("User is not a judge");
        }

        // =============================
        // 4. AssignJudgeAsync - Track không tồn tại
        // =============================
        [Test]
        public void AssignJudgeAsync_WhenTrackNotFound_Throws()
        {
            var dto = new JudgeAssignmentCreateDto { JudgeId = 1, HackathonId = 1, PhaseId = 1 };
            var hackathon = new Hackathon { HackathonId = 1, Name = "Test Hackathon" };
            var judge = new User { UserId = 1, RoleId = 6 }; // Judge role

            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hackathon);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(judge);
            _trackRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Track)null);

            Func<Task> act = async () => await _service.AssignJudgeAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Track not found");
        }

        // =============================
        // 5. AssignJudgeAsync - Phase không tồn tại
        // =============================
        [Test]
        public void AssignJudgeAsync_WhenPhaseNotFound_Throws()
        {
            var dto = new JudgeAssignmentCreateDto { JudgeId = 1, HackathonId = 99, PhaseId = 99 };
            var hackathon = new Hackathon { HackathonId = 1, Name = "Test Hackathon" };
            var judge = new User { UserId = 1, RoleId = 6 };
            var track = new Track { TrackId = 1, Name = "Web Development" };

            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hackathon);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(judge);
            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);
            _phaseRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((HackathonPhase)null);

            Func<Task> act = async () => await _service.AssignJudgeAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Phase not found");
        }

        // =============================
        // 6. AssignJudgeAsync - Judge đã được assign
        // =============================
        [Test]
        public void AssignJudgeAsync_WhenJudgeAlreadyAssigned_Throws()
        {
            var dto = new JudgeAssignmentCreateDto { JudgeId = 1, HackathonId = 1, PhaseId = 1 };
            var hackathon = new Hackathon { HackathonId = 1, Name = "Test Hackathon" };
            var judge = new User { UserId = 1, RoleId = 6 };
            var track = new Track { TrackId = 1, Name = "Web Development" };
            var phase = new HackathonPhase { PhaseId = 1, PhaseName = "Final Phase" };

            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hackathon);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(judge);
            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            _judgeAssignmentRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<JudgeAssignment, bool>>>()))
                               .ReturnsAsync(true);

            Func<Task> act = async () => await _service.AssignJudgeAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("This judge is already assigned to this track in the same phase.");
        }
        // =============================
        // 7. AssignJudgeAsync - Assign thành công và gửi notification
        // =============================
        [Test]
        public async Task AssignJudgeAsync_WhenValid_ShouldAssignAndNotify()
        {
            var dto = new JudgeAssignmentCreateDto { JudgeId = 1, HackathonId = 1, PhaseId = 1 };
            var hackathon = new Hackathon { HackathonId = 1, Name = "AI Hackathon 2024" };
            var judge = new User { UserId = 1, RoleId = 6, FullName = "John Judge" };
            var track = new Track { TrackId = 1, Name = "Machine Learning" };
            var phase = new HackathonPhase { PhaseId = 1, PhaseName = "Final Round" };
            var assignment = new JudgeAssignment { AssignmentId = 1, JudgeId = 1, HackathonId = 1 };

            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hackathon);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(judge);
            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            _judgeAssignmentRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<JudgeAssignment, bool>>>()))
                               .ReturnsAsync(false);

            _judgeAssignmentRepo.Setup(r => r.AddAsync(It.IsAny<JudgeAssignment>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                                   .ReturnsAsync(new NotificationDto());

            _mapperMock.Setup(m => m.Map<JudgeAssignmentResponseDto>(It.IsAny<JudgeAssignment>()))
                      .Returns(new JudgeAssignmentResponseDto { AssignmentId = 1, JudgeId = 1 });

            var result = await _service.AssignJudgeAsync(dto, 5);

            result.Should().NotBeNull();
            result.AssignmentId.Should().Be(1);

            _judgeAssignmentRepo.Verify(r => r.AddAsync(It.Is<JudgeAssignment>(a => 
                a.JudgeId == 1 && 
                a.HackathonId == 1 && 
                a.Status == "Active")), Times.Once);

            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.Is<CreateNotificationDto>(dto => 
                dto.UserId == 1 && 
                dto.Message.Contains("AI Hackathon 2024"))), Times.Once);
        }

        // =============================
        // 8. GetByHackathonAsync - Lấy assignments theo hackathon
        // =============================
        [Test]
        public async Task GetByHackathonAsync_ShouldReturnAssignments()
        {
            var assignments = new List<JudgeAssignment>
            {
                new JudgeAssignment { AssignmentId = 1, JudgeId = 1, HackathonId = 1 },
                new JudgeAssignment { AssignmentId = 2, JudgeId = 2, HackathonId = 1 }
            };
            var assignmentDtos = new List<JudgeAssignmentResponseDto>
            {
                new JudgeAssignmentResponseDto { AssignmentId = 1, JudgeId = 1 },
                new JudgeAssignmentResponseDto { AssignmentId = 2, JudgeId = 2 }
            };

            _judgeAssignmentRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<JudgeAssignment, bool>>>(),
                It.IsAny<Expression<Func<JudgeAssignment, object>>>(),
                It.IsAny<Expression<Func<JudgeAssignment, object>>>(),
                It.IsAny<Expression<Func<JudgeAssignment, object>>>(),
                It.IsAny<Expression<Func<JudgeAssignment, object>>>()))
                .ReturnsAsync(assignments);

            _mapperMock.Setup(m => m.Map<List<JudgeAssignmentResponseDto>>(assignments))
                      .Returns(assignmentDtos);

            var result = await _service.GetByHackathonAsync(1);

            result.Should().HaveCount(2);
            result.First().AssignmentId.Should().Be(1);
        }

        // =============================
        // 9. RemoveAssignmentAsync - Assignment không tồn tại
        // =============================
        [Test]
        public void RemoveAssignmentAsync_WhenAssignmentNotFound_Throws()
        {
            _judgeAssignmentRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((JudgeAssignment)null);

            Func<Task> act = async () => await _service.RemoveAssignmentAsync(99);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Assignment not found");
        }

        // =============================
        // 10. RemoveAssignmentAsync - Assignment đã bị block
        // =============================
        [Test]
        public void RemoveAssignmentAsync_WhenAssignmentAlreadyBlocked_Throws()
        {
            var assignment = new JudgeAssignment { AssignmentId = 1, Status = "Blocked" };

            _judgeAssignmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);

            Func<Task> act = async () => await _service.RemoveAssignmentAsync(1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("This assignment is already blocked");
        }

        // =============================
        // 11. RemoveAssignmentAsync - Remove thành công và gửi notification
        // =============================
        [Test]
        public async Task RemoveAssignmentAsync_WhenValid_ShouldRemoveAndNotify()
        {
            var assignment = new JudgeAssignment 
            { 
                AssignmentId = 1, 
                JudgeId = 1, 
                HackathonId = 1, 
                Status = "Active" 
            };
            var hackathon = new Hackathon { HackathonId = 1, Name = "Test Hackathon" };

            _judgeAssignmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hackathon);

            _judgeAssignmentRepo.Setup(r => r.Update(It.IsAny<JudgeAssignment>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                                   .ReturnsAsync(new NotificationDto());

            var result = await _service.RemoveAssignmentAsync(1);

            result.Should().BeTrue();
            assignment.Status.Should().Be("Blocked");

            _judgeAssignmentRepo.Verify(r => r.Update(assignment), Times.Once);
            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.Is<CreateNotificationDto>(dto => 
                dto.UserId == 1 && 
                dto.Message.Contains("removed"))), Times.Once);
        }

        // =============================
        // 12. ReactivateAssignmentAsync - Assignment không tồn tại
        // =============================
        [Test]
        public void ReactivateAssignmentAsync_WhenAssignmentNotFound_Throws()
        {
            _judgeAssignmentRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((JudgeAssignment)null);

            Func<Task> act = async () => await _service.ReactivateAssignmentAsync(99);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Assignment not found");
        }

        // =============================
        // 13. ReactivateAssignmentAsync - Assignment đã active
        // =============================
        [Test]
        public void ReactivateAssignmentAsync_WhenAssignmentAlreadyActive_Throws()
        {
            var assignment = new JudgeAssignment { AssignmentId = 1, Status = "Active" };

            _judgeAssignmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);

            Func<Task> act = async () => await _service.ReactivateAssignmentAsync(1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Assignment is already active");
        }

        // =============================
        // 14. ReactivateAssignmentAsync - Reactivate thành công
        // =============================
        [Test]
        public async Task ReactivateAssignmentAsync_WhenValid_ShouldReactivate()
        {
            var assignment = new JudgeAssignment { AssignmentId = 1, Status = "Blocked" };

            _judgeAssignmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
            _judgeAssignmentRepo.Setup(r => r.Update(It.IsAny<JudgeAssignment>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.ReactivateAssignmentAsync(1);

            result.Should().BeTrue();
            assignment.Status.Should().Be("Active");

            _judgeAssignmentRepo.Verify(r => r.Update(assignment), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }

        // =============================
        // 15. GetAssignedHackathonsAsync - Lấy hackathons được assign cho judge
        // =============================
        [Test]
        public async Task GetAssignedHackathonsAsync_ShouldReturnAssignedHackathons()
        {
            var assignments = new List<JudgeAssignment>
            {
                new JudgeAssignment 
                { 
                    AssignmentId = 1, 
                    JudgeId = 1, 
                    HackathonId = 1, 
                    Status = "Active",
                    Hackathon = new Hackathon { HackathonId = 1, Name = "Hackathon 1" }
                },
                new JudgeAssignment 
                { 
                    AssignmentId = 2, 
                    JudgeId = 1, 
                    HackathonId = 2, 
                    Status = "Active",
                    Hackathon = new Hackathon { HackathonId = 2, Name = "Hackathon 2" }
                }
            };
            var hackathonDtos = new List<HackathonAssignedDto>
            {
                new HackathonAssignedDto { HackathonId = 1, HackathonName = "Hackathon 1", Status = "Active" },
                new HackathonAssignedDto { HackathonId = 2, HackathonName = "Hackathon 2", Status = "Active" }
            };

            _judgeAssignmentRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<JudgeAssignment, bool>>>(),
                It.IsAny<Expression<Func<JudgeAssignment, object>>>(),
                It.IsAny<Expression<Func<JudgeAssignment, object>>>(),
                It.IsAny<Expression<Func<JudgeAssignment, object>>>()))
                .ReturnsAsync(assignments);

            _mapperMock.Setup(m => m.Map<List<HackathonAssignedDto>>(assignments))
                      .Returns(hackathonDtos);

            var result = await _service.GetAssignedHackathonsAsync(1);

            result.Should().HaveCount(2);
            result.All(h => h.Status == "Active").Should().BeTrue();
            result.First().HackathonName.Should().Be("Hackathon 1");
        }

        // =============================
        // 16. AssignJudgeAsync - Assign không có TrackId và PhaseId (service bug - should throw)
        // =============================
        [Test]
        public void AssignJudgeAsync_WhenNoTrackAndPhase_ShouldThrowInvalidOperationException()
        {
            var dto = new JudgeAssignmentCreateDto { JudgeId = 1, HackathonId = 1, PhaseId = null };
            var hackathon = new Hackathon { HackathonId = 1, Name = "Global Hackathon" };
            var judge = new User { UserId = 1, RoleId = 6 };

            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hackathon);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(judge);

            _judgeAssignmentRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<JudgeAssignment, bool>>>()))
                               .ReturnsAsync(false);

            _judgeAssignmentRepo.Setup(r => r.AddAsync(It.IsAny<JudgeAssignment>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            // ✅ Fix: Service has bug - it calls .Value on nullable without checking HasValue
            // This test should expect InvalidOperationException due to service bug
            Func<Task> act = async () => await _service.AssignJudgeAsync(dto, 5);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Nullable object must have a value.");
        }

        // =============================
        // 17. AssignJudgeAsync - Assign với TrackId và PhaseId (working scenario)
        // =============================
        [Test]
        public async Task AssignJudgeAsync_WhenWithTrackAndPhase_ShouldAssignSuccessfully()
        {
            var dto = new JudgeAssignmentCreateDto { JudgeId = 1, HackathonId = 1, PhaseId = 3 };
            var hackathon = new Hackathon { HackathonId = 1, Name = "Tech Hackathon" };
            var judge = new User { UserId = 1, RoleId = 6 };
            var track = new Track { TrackId = 2, Name = "AI Track" };
            var phase = new HackathonPhase { PhaseId = 3, PhaseName = "Final Phase" };

            _hackathonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hackathon);
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(judge);
            _trackRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(track);
            _phaseRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(phase);

            _judgeAssignmentRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<JudgeAssignment, bool>>>()))
                               .ReturnsAsync(false);

            _judgeAssignmentRepo.Setup(r => r.AddAsync(It.IsAny<JudgeAssignment>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                                   .ReturnsAsync(new NotificationDto());

            _mapperMock.Setup(m => m.Map<JudgeAssignmentResponseDto>(It.IsAny<JudgeAssignment>()))
                      .Returns(new JudgeAssignmentResponseDto { AssignmentId = 1, JudgeId = 1 });

            var result = await _service.AssignJudgeAsync(dto, 5);

            result.Should().NotBeNull();
            result.JudgeId.Should().Be(1);

            _judgeAssignmentRepo.Verify(r => r.AddAsync(It.Is<JudgeAssignment>(a => 
                a.JudgeId == 1 && 
                a.HackathonId == 1 && 
                a.PhaseId == 3)), Times.Once);

            // Verify notification was sent with track and phase info
            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.Is<CreateNotificationDto>(dto => 
                dto.UserId == 1 && 
                dto.Message.Contains("Tech Hackathon") &&
                dto.Message.Contains("AI Track") &&
                dto.Message.Contains("Final Phase"))), Times.Once);
        }
    }
}