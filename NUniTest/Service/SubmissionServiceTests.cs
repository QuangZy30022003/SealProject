using AutoMapper;
using Common.DTOs.Submission;
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
    public class SubmissionServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<INotificationService> _notificationServiceMock;
        private SubmissionService _service;

        private Mock<IRepository<Submission>> _submissionRepo;
        private Mock<IRepository<Team>> _teamRepo;
        private Mock<IRepository<HackathonPhase>> _phaseRepo;
        private Mock<IRepository<JudgeAssignment>> _judgeAssignmentRepo;
        private Mock<IRepository<Track>> _trackRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _notificationServiceMock = new Mock<INotificationService>();

            _submissionRepo = new Mock<IRepository<Submission>>();
            _teamRepo = new Mock<IRepository<Team>>();
            _phaseRepo = new Mock<IRepository<HackathonPhase>>();
            _judgeAssignmentRepo = new Mock<IRepository<JudgeAssignment>>();
            _trackRepo = new Mock<IRepository<Track>>();

            _uowMock.Setup(u => u.Submissions).Returns(_submissionRepo.Object);
            _uowMock.Setup(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.Setup(u => u.HackathonPhases).Returns(_phaseRepo.Object);
            _uowMock.Setup(u => u.JudgeAssignments).Returns(_judgeAssignmentRepo.Object);
            _uowMock.Setup(u => u.Tracks).Returns(_trackRepo.Object);

            _service = new SubmissionService(_uowMock.Object, _mapperMock.Object, _notificationServiceMock.Object);
        }

        // =============================
        // 1. CreateDraftAsync - Team không tồn tại
        // =============================
        [Test]
        public void CreateDraftAsync_WhenTeamNotFound_Throws()
        {
            var dto = new SubmissionCreateDto { TeamId = 99, PhaseId = 1, Title = "Test", FilePath = "/path" };

            _teamRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Team)null);

            Func<Task> act = async () => await _service.CreateDraftAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team not found");
        }

        // =============================
        // 2. CreateDraftAsync - Phase không tồn tại
        // =============================
        [Test]
        public void CreateDraftAsync_WhenPhaseNotFound_Throws()
        {
            var dto = new SubmissionCreateDto { TeamId = 1, PhaseId = 99, Title = "Test", FilePath = "/path" };
            var team = new Team { TeamId = 1 };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _phaseRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((HackathonPhase)null);

            Func<Task> act = async () => await _service.CreateDraftAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Phase not found");
        }

        // =============================
        // 3. CreateDraftAsync - Tạo draft thành công
        // =============================
        [Test]
        public async Task CreateDraftAsync_WhenValid_ShouldCreateDraft()
        {
            var dto = new SubmissionCreateDto { TeamId = 1, PhaseId = 1, Title = "Test Submission", FilePath = "/uploads/test.pdf" };
            var team = new Team { TeamId = 1 };
            var phase = new HackathonPhase { PhaseId = 1 };
            var submission = new Submission { SubmissionId = 1, TeamId = 1, IsFinal = false };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            _submissionRepo.Setup(r => r.AddAsync(It.IsAny<Submission>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<SubmissionResponseDto>(It.IsAny<Submission>()))
                      .Returns(new SubmissionResponseDto { SubmissionId = 1, Title = "Test Submission", IsFinal = false });

            var result = await _service.CreateDraftAsync(dto, 5);

            result.Should().NotBeNull();
            result.SubmissionId.Should().Be(1);
            result.IsFinal.Should().BeFalse();

            _submissionRepo.Verify(r => r.AddAsync(It.Is<Submission>(s => 
                s.TeamId == 1 && 
                s.PhaseId == 1 && 
                s.SubmittedBy == 5 && 
                s.IsFinal == false)), Times.Once);
        }

        // =============================
        // 4. UpdateDraftAsync - Submission không tồn tại
        // =============================
        [Test]
        public void UpdateDraftAsync_WhenSubmissionNotFound_Throws()
        {
            var dto = new SubmissionUpdateDto { Title = "Updated", FilePath = "/new/path" };

            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Submission)null);

            Func<Task> act = async () => await _service.UpdateDraftAsync(99, dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Submission not found");
        }

        // =============================
        // 5. UpdateDraftAsync - Submission đã final
        // =============================
        [Test]
        public void UpdateDraftAsync_WhenSubmissionIsFinal_Throws()
        {
            var dto = new SubmissionUpdateDto { Title = "Updated", FilePath = "/new/path" };
            var submission = new Submission { SubmissionId = 1, IsFinal = true };

            _submissionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);

            Func<Task> act = async () => await _service.UpdateDraftAsync(1, dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Cannot edit final submission");
        }

        // =============================
        // 6. UpdateDraftAsync - User không phải người submit
        // =============================
        [Test]
        public void UpdateDraftAsync_WhenUserNotAuthorized_Throws()
        {
            var dto = new SubmissionUpdateDto { Title = "Updated", FilePath = "/new/path" };
            var submission = new Submission { SubmissionId = 1, IsFinal = false, SubmittedBy = 5 };

            _submissionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);

            Func<Task> act = async () => await _service.UpdateDraftAsync(1, dto, 10); // User khác

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Not authorized to edit this draft");
        }
        // =============================
        // 7. UpdateDraftAsync - Update thành công
        // =============================
        [Test]
        public async Task UpdateDraftAsync_WhenValid_ShouldUpdate()
        {
            var dto = new SubmissionUpdateDto { Title = "Updated Title", FilePath = "/updated/path.pdf" };
            var submission = new Submission 
            { 
                SubmissionId = 1, 
                IsFinal = false, 
                SubmittedBy = 5,
                Title = "Original Title",
                FilePath = "/original/path.pdf"
            };

            _submissionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<SubmissionResponseDto>(submission))
                      .Returns(new SubmissionResponseDto { SubmissionId = 1, Title = "Updated Title" });

            var result = await _service.UpdateDraftAsync(1, dto, 5);

            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Title");

            submission.Title.Should().Be("Updated Title");
            submission.FilePath.Should().Be("/updated/path.pdf");

            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }

        // =============================
        // 8. SetFinalAsync - Submission không tồn tại
        // =============================
        [Test]
        public void SetFinalAsync_WhenSubmissionNotFound_Throws()
        {
            var dto = new SubmissionFinalDto { SubmissionId = 99, TeamId = 1 };

            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Submission)null);

            Func<Task> act = async () => await _service.SetFinalAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Submission not found");
        }

        // =============================
        // 9. SetFinalAsync - Team không tồn tại
        // =============================
        [Test]
        public void SetFinalAsync_WhenTeamNotFound_Throws()
        {
            var dto = new SubmissionFinalDto { SubmissionId = 1, TeamId = 99 };
            var submission = new Submission { SubmissionId = 1, TeamId = 99 };

            _submissionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);
            _teamRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Team)null);

            Func<Task> act = async () => await _service.SetFinalAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team not found");
        }

        // =============================
        // 10. SetFinalAsync - User không phải team leader
        // =============================
        [Test]
        public void SetFinalAsync_WhenUserNotTeamLeader_Throws()
        {
            var dto = new SubmissionFinalDto { SubmissionId = 1, TeamId = 1 };
            var submission = new Submission { SubmissionId = 1, TeamId = 1 };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };

            _submissionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.SetFinalAsync(dto, 10); // User khác

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Not authorized to set final submission");
        }

        // =============================
        // 11. SetFinalAsync - Set final thành công và gửi notification
        // =============================
        [Test]
        public async Task SetFinalAsync_WhenValid_ShouldSetFinalAndNotifyJudges()
        {
            var dto = new SubmissionFinalDto { SubmissionId = 1, TeamId = 1 };
            var submission = new Submission { SubmissionId = 1, TeamId = 1, PhaseId = 2, IsFinal = false };
            var team = new Team { TeamId = 1, TeamLeaderId = 5, TeamName = "Alpha Team" };
            var judgeAssignments = new List<JudgeAssignment>
            {
                new JudgeAssignment { JudgeId = 10, PhaseId = 2 },
                new JudgeAssignment { JudgeId = 11, PhaseId = 2 }
            };

            _submissionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);
            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _judgeAssignmentRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<JudgeAssignment, bool>>>(),
        It.IsAny<Func<IQueryable<JudgeAssignment>, IOrderedQueryable<JudgeAssignment>>>(),
        It.IsAny<string>()))
    .ReturnsAsync(judgeAssignments);


            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _notificationServiceMock.Setup(n => n.CreateNotificationsAsync(
                It.IsAny<List<int>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mapperMock.Setup(m => m.Map<SubmissionResponseDto>(submission))
                      .Returns(new SubmissionResponseDto { SubmissionId = 1, IsFinal = true });

            var result = await _service.SetFinalAsync(dto, 5);

            result.Should().NotBeNull();
            result.IsFinal.Should().BeTrue();

            submission.IsFinal.Should().BeTrue();

            _notificationServiceMock.Verify(n => n.CreateNotificationsAsync(
                It.Is<List<int>>(ids => ids.Contains(10) && ids.Contains(11)),
                It.Is<string>(msg => msg.Contains("Alpha Team"))), Times.Once);
        }

        // =============================
        // 12. GetSubmissionsByTeamAsync - Team không tồn tại
        // =============================
        [Test]
        public void GetSubmissionsByTeamAsync_WhenTeamNotFound_Throws()
        {
            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                    .ReturnsAsync(false);

            Func<Task> act = async () => await _service.GetSubmissionsByTeamAsync(99);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team not found");
        }

        // =============================
        // 13. GetSubmissionsByTeamAsync - Lấy submissions của team thành công
        // =============================
        [Test]
        public async Task GetSubmissionsByTeamAsync_WhenValid_ShouldReturnSubmissions()
        {
            var submissions = new List<Submission>
            {
                new Submission 
                { 
                    SubmissionId = 1, 
                    TeamId = 1, 
                    Title = "Submission 1",
                    Team = new Team 
                    { 
                        TeamId = 1, 
                        TeamTrackSelections = new List<TeamTrackSelection>
                        {
                            new TeamTrackSelection { TrackId = 1 }
                        }
                    }
                }
            };
            var tracks = new List<Track>
            {
                new Track { TrackId = 1, Name = "Web Development" }
            };
            var submissionDtos = new List<SubmissionResponseDto>
            {
                new SubmissionResponseDto { SubmissionId = 1, Title = "Submission 1" }
            };

            _teamRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Team, bool>>>()))
                    .ReturnsAsync(true);

            _submissionRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Submission, bool>>>(),
                It.IsAny<Expression<Func<Submission, object>>>(),
                It.IsAny<Expression<Func<Submission, object>>>(),
                It.IsAny<Expression<Func<Submission, object>>>()))
                .ReturnsAsync(submissions);

            _trackRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Track, bool>>>(), null, null))
                     .ReturnsAsync(tracks);

            _mapperMock.Setup(m => m.Map<List<SubmissionResponseDto>>(submissions))
                      .Returns(submissionDtos);

            var result = await _service.GetSubmissionsByTeamAsync(1);

            result.Should().HaveCount(1);
            result.First().SubmissionId.Should().Be(1);
        }

        // =============================
        // 14. GetFinalSubmissionsByPhaseAsync - Judge không được assign
        // =============================
        [Test]
        public void GetFinalSubmissionsByPhaseAsync_WhenJudgeNotAssigned_Throws()
        {
            _judgeAssignmentRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<JudgeAssignment, bool>>>()))
                               .ReturnsAsync(false);

            Func<Task> act = async () => await _service.GetFinalSubmissionsByPhaseAsync(1, 10, "Judge");

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not assigned to this phase.");
        }

        // =============================
        // 15. GetFinalSubmissionsByPhaseAsync - Role không hợp lệ
        // =============================
        [Test]
        public void GetFinalSubmissionsByPhaseAsync_WhenInvalidRole_Throws()
        {
            Func<Task> act = async () => await _service.GetFinalSubmissionsByPhaseAsync(1, 10, "Student");

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not authorized to view submissions.");
        }

        // =============================
        // 16. GetFinalSubmissionsByPhaseAsync - Admin lấy submissions thành công
        // =============================
        [Test]
        public async Task GetFinalSubmissionsByPhaseAsync_WhenAdmin_ShouldReturnSubmissions()
        {
            var submissions = new List<Submission>
            {
                new Submission 
                { 
                    SubmissionId = 1, 
                    PhaseId = 1, 
                    IsFinal = true,
                    Team = new Team 
                    { 
                        TeamId = 1, 
                        TeamTrackSelections = new List<TeamTrackSelection>
                        {
                            new TeamTrackSelection { TrackId = 1 }
                        }
                    }
                }
            };
            var tracks = new List<Track>
            {
                new Track { TrackId = 1, Name = "AI/ML" }
            };
            var submissionDtos = new List<SubmissionResponseDto>
            {
                new SubmissionResponseDto { SubmissionId = 1, IsFinal = true }
            };

            _submissionRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Submission, bool>>>(),
                It.IsAny<Expression<Func<Submission, object>>>(),
                It.IsAny<Expression<Func<Submission, object>>>(),
                It.IsAny<Expression<Func<Submission, object>>>()))
                .ReturnsAsync(submissions);

            _trackRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Track, bool>>>(), null, null))
                     .ReturnsAsync(tracks);

            _mapperMock.Setup(m => m.Map<List<SubmissionResponseDto>>(submissions))
                      .Returns(submissionDtos);

            var result = await _service.GetFinalSubmissionsByPhaseAsync(1, 5, "Admin");

            result.Should().HaveCount(1);
            result.First().IsFinal.Should().BeTrue();
        }
    }
}