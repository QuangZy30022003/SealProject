using AutoMapper;
using Common.DTOs.AppealDto;
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
    public class AppealServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<INotificationService> _notificationServiceMock;
        private Mock<IScoreService> _scoreServiceMock;
        private AppealService _service;

        private Mock<IRepository<Appeal>> _appealRepo;
        private Mock<IRepository<Team>> _teamRepo;
        private Mock<IRepository<TeamMember>> _teamMemberRepo;
        private Mock<IRepository<PenaltiesBonuse>> _penaltyRepo;
        private Mock<IRepository<Submission>> _submissionRepo;
        private Mock<IRepository<Score>> _scoreRepo;
        private Mock<IRepository<ScoreHistory>> _scoreHistoryRepo;
        private Mock<IRepository<HackathonPhase>> _phaseRepo;
        private Mock<IRepository<User>> _userRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _notificationServiceMock = new Mock<INotificationService>();
            _scoreServiceMock = new Mock<IScoreService>();

            _appealRepo = new Mock<IRepository<Appeal>>();
            _teamRepo = new Mock<IRepository<Team>>();
            _teamMemberRepo = new Mock<IRepository<TeamMember>>();
            _penaltyRepo = new Mock<IRepository<PenaltiesBonuse>>();
            _submissionRepo = new Mock<IRepository<Submission>>();
            _scoreRepo = new Mock<IRepository<Score>>();
            _scoreHistoryRepo = new Mock<IRepository<ScoreHistory>>();
            _phaseRepo = new Mock<IRepository<HackathonPhase>>();
            _userRepo = new Mock<IRepository<User>>();

            _uowMock.Setup(u => u.Appeals).Returns(_appealRepo.Object);
            _uowMock.Setup(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.Setup(u => u.TeamMembers).Returns(_teamMemberRepo.Object);
            _uowMock.Setup(u => u.PenaltiesBonuses).Returns(_penaltyRepo.Object);
            _uowMock.Setup(u => u.Submissions).Returns(_submissionRepo.Object);
            _uowMock.Setup(u => u.Scores).Returns(_scoreRepo.Object);
            _uowMock.Setup(u => u.ScoreHistorys).Returns(_scoreHistoryRepo.Object);
            _uowMock.Setup(u => u.HackathonPhases).Returns(_phaseRepo.Object);
            _uowMock.Setup(u => u.Users).Returns(_userRepo.Object);


            _service = new AppealService(_uowMock.Object, _mapperMock.Object, _notificationServiceMock.Object, _scoreServiceMock.Object);
        }

        // =============================
        // 1. CreateAppealAsync - User không phải team member
        // =============================
        [Test]
        public void CreateAppealAsync_WhenUserNotTeamMember_Throws()
        {
            var dto = new CreateAppealDto { TeamId = 1, AppealType = AppealType.Penalty };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(false);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not a member of this team.");
        }

        // =============================
        // 2. CreateAppealAsync - User không phải team leader
        // =============================
        [Test]
        public void CreateAppealAsync_WhenUserNotTeamLeader_Throws()
        {
            var dto = new CreateAppealDto { TeamId = 1, AppealType = AppealType.Penalty };
            var team = new Team { TeamId = 1, TeamLeaderId = 10 };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Only the team leader can create appeals.");
        }

        // =============================
        // 3. CreateAppealAsync - Penalty appeal thiếu AdjustmentId
        // =============================
        [Test]
        public void CreateAppealAsync_WhenPenaltyAppealMissingAdjustmentId_Throws()
        {
            var dto = new CreateAppealDto { TeamId = 1, AppealType = AppealType.Penalty };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("AdjustmentId is required for penalty appeals.");
        }

        // =============================
        // 4. CreateAppealAsync - Score appeal thiếu SubmissionId hoặc JudgeId
        // =============================
        [Test]
        public void CreateAppealAsync_WhenScoreAppealMissingFields_Throws()
        {
            var dto = new CreateAppealDto { TeamId = 1, AppealType = AppealType.Score };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("SubmissionId and JudgeId are required for score appeals.");
        }

        // =============================
        // 5. CreateAppealAsync - Penalty không tồn tại
        // =============================
        [Test]
        public void CreateAppealAsync_WhenPenaltyNotFound_Throws()
        {
            var dto = new CreateAppealDto 
            { 
                TeamId = 1, 
                AppealType = AppealType.Penalty, 
                AdjustmentId = 99 
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _penaltyRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PenaltiesBonuse)null);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Penalty/Bonus not found.");
        }

        // =============================
        // 6. CreateAppealAsync - Penalty thuộc team khác
        // =============================
        [Test]
        public void CreateAppealAsync_WhenPenaltyBelongsToOtherTeam_Throws()
        {
            var dto = new CreateAppealDto 
            { 
                TeamId = 1, 
                AppealType = AppealType.Penalty, 
                AdjustmentId = 99 
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };
            var penalty = new PenaltiesBonuse { AdjustmentId = 99, TeamId = 2 };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _penaltyRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(penalty);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You cannot appeal adjustment belonging to another team.");
        }

        // =============================
        // 7. CreateAppealAsync - Penalty appeal quá deadline (7 ngày)
        // =============================
        [Test]
        public void CreateAppealAsync_WhenPenaltyAppealPastDeadline_Throws()
        {
            var dto = new CreateAppealDto 
            { 
                TeamId = 1, 
                AppealType = AppealType.Penalty, 
                AdjustmentId = 99 
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };
            var penalty = new PenaltiesBonuse 
            { 
                AdjustmentId = 99, 
                TeamId = 1, 
                CreatedAt = DateTime.UtcNow.AddDays(-8) // 8 ngày trước
            };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _penaltyRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(penalty);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Appeal deadline has passed. You can only appeal within 7 days of penalty creation.");
        }
        // =============================
        // 8. CreateAppealAsync - Submission không tồn tại
        // =============================
        [Test]
        public void CreateAppealAsync_WhenSubmissionNotFound_Throws()
        {
            var dto = new CreateAppealDto 
            { 
                TeamId = 1, 
                AppealType = AppealType.Score, 
                SubmissionId = 99,
                JudgeId = 10
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Submission)null);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Submission not found.");
        }

        // =============================
        // 9. CreateAppealAsync - Submission thuộc team khác
        // =============================
        [Test]
        public void CreateAppealAsync_WhenSubmissionBelongsToOtherTeam_Throws()
        {
            var dto = new CreateAppealDto 
            { 
                TeamId = 1, 
                AppealType = AppealType.Score, 
                SubmissionId = 99,
                JudgeId = 10
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };
            var submission = new Submission { SubmissionId = 99, TeamId = 2 };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(submission);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You cannot appeal score belonging to another team.");
        }

        // =============================
        // 10. CreateAppealAsync - Score không tồn tại cho submission + judge
        // =============================
        [Test]
        public void CreateAppealAsync_WhenScoreNotFound_Throws()
        {
            var dto = new CreateAppealDto 
            { 
                TeamId = 1, 
                AppealType = AppealType.Score, 
                SubmissionId = 99,
                JudgeId = 10
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };
            var submission = new Submission { SubmissionId = 99, TeamId = 1 };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(submission);

            _scoreRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Score, bool>>>()))
                     .ReturnsAsync(false);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Score not found for this Submission + Judge.");
        }
        // =============================
        // 11. CreateAppealAsync - Score appeal quá deadline (7 ngày sau khi phase kết thúc)
        // =============================
        [Test]
        public void CreateAppealAsync_WhenScoreAppealPastDeadline_Throws()
        {
            var dto = new CreateAppealDto 
            { 
                TeamId = 1, 
                AppealType = AppealType.Score, 
                SubmissionId = 99,
                JudgeId = 10
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };
            var submission = new Submission { SubmissionId = 99, TeamId = 1, PhaseId = 1 };
            var phase = new HackathonPhase 
            { 
                PhaseId = 1, 
                EndDate = DateTime.UtcNow.AddDays(-8), // Phase kết thúc 8 ngày trước
                HackathonId = 1,
                PhaseName = "Final Phase"
            };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(submission);

            _scoreRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Score, bool>>>()))
                     .ReturnsAsync(true);

            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<Exception>()
                .WithMessage(It.Is<string>(s => s.Contains("Appeal deadline has passed")));
        }

        // =============================
        // 12. CreateAppealAsync - Duplicate pending appeal
        // =============================
        [Test]
        public void CreateAppealAsync_WhenDuplicatePendingAppeal_Throws()
        {
            var dto = new CreateAppealDto 
            { 
                TeamId = 1, 
                AppealType = AppealType.Penalty, 
                AdjustmentId = 99 
            };
            var team = new Team { TeamId = 1, TeamLeaderId = 5 };
            var penalty = new PenaltiesBonuse 
            { 
                AdjustmentId = 99, 
                TeamId = 1, 
                CreatedAt = DateTime.UtcNow.AddDays(-1) // 1 ngày trước
            };

            _teamMemberRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                          .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _penaltyRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(penalty);

            _appealRepo.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Appeal, bool>>>()))
                      .ReturnsAsync(true);

            Func<Task> act = async () => await _service.CreateAppealAsync(dto, 5);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("An active pending appeal already exists for this target.");
        }
        // =============================
        // 13. CreateAppealAsync - Tạo penalty appeal thành công
        // =============================
        [Test]
        public async Task CreateAppealAsync_WhenValidPenaltyAppeal_ShouldCreate()
        {
            var dto = new CreateAppealDto
            {
                TeamId = 1,
                AppealType = AppealType.Penalty,
                AdjustmentId = 99,
                Message = "This penalty is unfair",
                Reason = "Wrong calculation"
            };

            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 5,
                TeamName = "Alpha"
            };

            var penalty = new PenaltiesBonuse
            {
                AdjustmentId = 99,
                TeamId = 1,
                PhaseId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var appeal = new Appeal
            {
                AppealId = 1,
                TeamId = 1,
                AppealType = AppealType.Penalty,
                Team = team, // ✅ Populate Team để tránh null reference
                Adjustment = penalty
            };

            var admins = new List<User>
    {
        new User { UserId = 10, RoleId = 2 }
    };

            // =========== FIX QUAN TRỌNG – GÁN REPO VÀO UOW ==============
            _uowMock.SetupGet(u => u.TeamMembers).Returns(_teamMemberRepo.Object);
            _uowMock.SetupGet(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.SetupGet(u => u.Users).Returns(_userRepo.Object);
            _uowMock.SetupGet(u => u.PenaltiesBonuses).Returns(_penaltyRepo.Object);
            _uowMock.SetupGet(u => u.Appeals).Returns(_appealRepo.Object);
            // ==============================================================

            // MOCK
            _teamMemberRepo.Setup(r =>
                    r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            _penaltyRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(penalty);

            _appealRepo.Setup(r =>
                    r.ExistsAsync(It.IsAny<Expression<Func<Appeal, bool>>>()))
                .ReturnsAsync(false);

            _appealRepo.Setup(r => r.AddAsync(It.IsAny<Appeal>()))
                       .Returns(Task.CompletedTask);

            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>()))
                    .ReturnsAsync(1);

            _appealRepo.Setup(r => r.GetByIdIncludingAsync(
                    It.IsAny<Expression<Func<Appeal, bool>>>(),
                    It.IsAny<Expression<Func<Appeal, object>>>(),
                    It.IsAny<Expression<Func<Appeal, object>>>(),
                    It.IsAny<Expression<Func<Appeal, object>>>(),
                    It.IsAny<Expression<Func<Appeal, object>>>(),
                    It.IsAny<Expression<Func<Appeal, object>>>()))
                .ReturnsAsync(appeal);

            _userRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), null, null))
         .ReturnsAsync(new List<User>   // danh sách admin
         {
             new User { UserId = 999, RoleId = 2 }
         });


            _notificationServiceMock.Setup(n =>
                    n.CreateNotificationsAsync(It.IsAny<List<int>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mapperMock.Setup(m => m.Map<AppealResponseDto>(appeal))
                       .Returns(new AppealResponseDto { AppealId = 1, TeamId = 1 });

            // RUN
            var result = await _service.CreateAppealAsync(dto, 5);

            // VERIFY
            result.AppealId.Should().Be(1);

            _appealRepo.Verify(r => r.AddAsync(It.IsAny<Appeal>()), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }


        // =============================
        // 14. CreateAppealAsync - Tạo score appeal thành công
        // =============================
        [Test]
        public async Task CreateAppealAsync_WhenValidScoreAppeal_ShouldCreate()
        {
            var dto = new CreateAppealDto
            {
                TeamId = 1,
                AppealType = AppealType.Score,
                SubmissionId = 99,
                JudgeId = 10,
                Message = "Score is too low",
                Reason = "Judge misunderstood our solution"
            };

            var team = new Team
            {
                TeamId = 1,
                TeamLeaderId = 5,
                TeamName = "Alpha"
            };

            var leaderUser = new User
            {
                UserId = 5,
                RoleId = 3,
                Email = "leader@mail.com"
            };

            var submission = new Submission
            {
                SubmissionId = 99,
                TeamId = 1,
                PhaseId = 1
            };

            var phase = new HackathonPhase
            {
                PhaseId = 1,
                HackathonId = 1,
                EndDate = DateTime.UtcNow.AddDays(-1),
                PhaseName = "Final Phase"
            };

            var admins = new List<User>
    {
        new User { UserId = 10, RoleId = 2 }
    };

            var appeal = new Appeal
            {
                AppealId = 2,
                TeamId = 1,
                AppealType = AppealType.Score,
                Team = team, // ✅ Populate Team để tránh null reference
                Submission = submission
            };

            // ================= MOCK =================

            _teamMemberRepo
                .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TeamMember, bool>>>()))
                .ReturnsAsync(true);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);

            // ⭐ BẮT BUỘC: mock leader để tránh NullReferenceException
            _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(leaderUser);

            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(submission);

            _scoreRepo.Setup(r =>
                    r.ExistsAsync(It.IsAny<Expression<Func<Score, bool>>>()))
                .ReturnsAsync(true);

            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            _appealRepo.Setup(r =>
                    r.ExistsAsync(It.IsAny<Expression<Func<Appeal, bool>>>()))
                .ReturnsAsync(false);

            _appealRepo.Setup(r => r.AddAsync(It.IsAny<Appeal>()))
                       .Returns(Task.CompletedTask);

            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>()))
                    .ReturnsAsync(1);

            _appealRepo.Setup(r => r.GetByIdIncludingAsync(
                    It.IsAny<Expression<Func<Appeal, bool>>>(),
                    It.IsAny<Expression<Func<Appeal, object>>>(),
                    It.IsAny<Expression<Func<Appeal, object>>>(),
                    It.IsAny<Expression<Func<Appeal, object>>>(),
                    It.IsAny<Expression<Func<Appeal, object>>>(),
                    It.IsAny<Expression<Func<Appeal, object>>>()))
                .ReturnsAsync(appeal);

            _userRepo.Setup(r =>
                    r.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), null, null))
                .ReturnsAsync(admins);

            _notificationServiceMock.Setup(n =>
                    n.CreateNotificationsAsync(It.IsAny<List<int>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mapperMock.Setup(m => m.Map<AppealResponseDto>(appeal))
                       .Returns(new AppealResponseDto { AppealId = 2, TeamId = 1 });

            // ================= RUN =================

            var result = await _service.CreateAppealAsync(dto, 5);

            // ================= ASSERT =================

            result.AppealId.Should().Be(2);

            _appealRepo.Verify(r => r.AddAsync(It.IsAny<Appeal>()), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
            _notificationServiceMock.Verify(n =>
                n.CreateNotificationsAsync(It.IsAny<List<int>>(), It.IsAny<string>()),
                Times.Once);
        }

        // =============================
        // 15. ReviewAppealAsync - Appeal không tồn tại
        // =============================
        [Test]
        public async Task ReviewAppealAsync_WhenAppealNotFound_ReturnsNull()
        {
            var dto = new ReviewAppealDto { Status = AppealStatus.Approved, AdminResponse = "Approved" };

            _appealRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Appeal)null);

            var result = await _service.ReviewAppealAsync(99, dto, 10);

            result.Should().BeNull();
        }

        // =============================
        // 16. ReviewAppealAsync - Appeal đã được review
        // =============================
        [Test]
        public void ReviewAppealAsync_WhenAppealAlreadyReviewed_Throws()
        {
            var dto = new ReviewAppealDto { Status = AppealStatus.Approved, AdminResponse = "Approved" };
            var appeal = new Appeal { AppealId = 1, Status = AppealStatus.Approved };

            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);

            Func<Task> act = async () => await _service.ReviewAppealAsync(1, dto, 10);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("This appeal has already been reviewed.");
        }

        // =============================
        // 17. ReviewAppealAsync - Approve penalty appeal thành công
        // =============================
        [Test]
        public async Task ReviewAppealAsync_WhenApprovePenaltyAppeal_ShouldRevertPenalty()
        {
            var dto = new ReviewAppealDto { Status = AppealStatus.Approved, AdminResponse = "Penalty reverted" };
            var appeal = new Appeal 
            { 
                AppealId = 1, 
                Status = AppealStatus.Pending, 
                AppealType = AppealType.Penalty,
                AdjustmentId = 99,
                TeamId = 1
            };
            var penalty = new PenaltiesBonuse 
            { 
                AdjustmentId = 99, 
                Points = -10, 
                Reason = "Late submission",
                IsDeleted = false
            };
            var teamMembers = new List<TeamMember> 
            { 
                new TeamMember { UserId = 5, TeamId = 1 },
                new TeamMember { UserId = 6, TeamId = 1 }
            };

            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Team { TeamId = 1 });

            _teamMemberRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<TeamMember, bool>>>(), null, null))
                          .ReturnsAsync(teamMembers);

            _penaltyRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(penalty);

            _appealRepo.Setup(r => r.Update(It.IsAny<Appeal>()));

            _penaltyRepo.Setup(r => r.Update(It.IsAny<PenaltiesBonuse>()));

            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _appealRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<Appeal, bool>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>()))
                .ReturnsAsync(appeal);

            _notificationServiceMock.Setup(n => n.CreateNotificationsAsync(
                It.IsAny<List<int>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mapperMock.Setup(m => m.Map<AppealResponseDto>(appeal))
                      .Returns(new AppealResponseDto { AppealId = 1, Status = AppealStatus.Approved });

            var result = await _service.ReviewAppealAsync(1, dto, 10);

            result.Status.Should().Be(AppealStatus.Approved);
            penalty.Points.Should().Be(0);
            penalty.Reason.Should().Contain("Reverted by approved appeal");

            _penaltyRepo.Verify(r => r.Update(penalty), Times.Once);
            _notificationServiceMock.Verify(n => n.CreateNotificationsAsync(
                It.IsAny<List<int>>(), It.IsAny<string>()), Times.Once);
        }
        // =============================
        // 18. ReviewAppealAsync - Approve score appeal thành công
        // =============================
        [Test]
        public async Task ReviewAppealAsync_WhenApproveScoreAppeal_ShouldMarkRequireReScore()
        {
            var dto = new ReviewAppealDto { Status = AppealStatus.Approved, AdminResponse = "Score will be reviewed" };
            var appeal = new Appeal 
            { 
                AppealId = 1, 
                Status = AppealStatus.Pending, 
                AppealType = AppealType.Score,
                SubmissionId = 99,
                JudgeId = 10,
                TeamId = 1
            };
            var scores = new List<Score>
            {
                new Score { ScoreId = 1, SubmissionId = 99, JudgeId = 10, CriteriaId = 1, Score1 = 8, Comment = "Good" },
                new Score { ScoreId = 2, SubmissionId = 99, JudgeId = 10, CriteriaId = 2, Score1 = 7, Comment = "Average" }
            };
            var teamMembers = new List<TeamMember> 
            { 
                new TeamMember { UserId = 5, TeamId = 1 },
                new TeamMember { UserId = 6, TeamId = 1 }
            };

            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Team { TeamId = 1 });

            _teamMemberRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<TeamMember, bool>>>(), null, null))
                          .ReturnsAsync(teamMembers);

            _scoreRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<Score, bool>>>(),
                It.IsAny<Func<IQueryable<Score>, IOrderedQueryable<Score>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(scores);


            _scoreHistoryRepo.Setup(r => r.AddAsync(It.IsAny<ScoreHistory>())).Returns(Task.CompletedTask);

            _scoreRepo.Setup(r => r.Update(It.IsAny<Score>()));

            _appealRepo.Setup(r => r.Update(It.IsAny<Appeal>()));

            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _appealRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<Appeal, bool>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>()))
                .ReturnsAsync(appeal);

            _notificationServiceMock.Setup(n => n.CreateNotificationsAsync(
                It.IsAny<List<int>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mapperMock.Setup(m => m.Map<AppealResponseDto>(appeal))
                      .Returns(new AppealResponseDto { AppealId = 1, Status = AppealStatus.Approved });

            var result = await _service.ReviewAppealAsync(1, dto, 10);

            result.Status.Should().Be(AppealStatus.Approved);

            _scoreHistoryRepo.Verify(r => r.AddAsync(It.IsAny<ScoreHistory>()), Times.Exactly(2));
            _scoreRepo.Verify(r => r.Update(It.IsAny<Score>()), Times.Exactly(2));
            _notificationServiceMock.Verify(n => n.CreateNotificationsAsync(
                It.IsAny<List<int>>(), It.IsAny<string>()), Times.Once);
        }
        // =============================
        // 19. ReviewAppealAsync - Reject appeal thành công
        // =============================
        [Test]
        public async Task ReviewAppealAsync_WhenRejectAppeal_ShouldSendNotification()
        {
            var dto = new ReviewAppealDto { Status = AppealStatus.Rejected, AdminResponse = "Not enough evidence" };
            var appeal = new Appeal 
            { 
                AppealId = 1, 
                Status = AppealStatus.Pending, 
                AppealType = AppealType.Penalty,
                TeamId = 1
            };
            var teamMembers = new List<TeamMember> 
            { 
                new TeamMember { UserId = 5, TeamId = 1 },
                new TeamMember { UserId = 6, TeamId = 1 }
            };

            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Team { TeamId = 1 });

            _teamMemberRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<TeamMember, bool>>>(), null, null))
                          .ReturnsAsync(teamMembers);

            _appealRepo.Setup(r => r.Update(It.IsAny<Appeal>()));

            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _appealRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<Appeal, bool>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>()))
                .ReturnsAsync(appeal);

            _notificationServiceMock.Setup(n => n.CreateNotificationsAsync(
                It.IsAny<List<int>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mapperMock.Setup(m => m.Map<AppealResponseDto>(appeal))
                      .Returns(new AppealResponseDto { AppealId = 1, Status = AppealStatus.Rejected });

            var result = await _service.ReviewAppealAsync(1, dto, 10);

            result.Status.Should().Be(AppealStatus.Rejected);

            _notificationServiceMock.Verify(n => n.CreateNotificationsAsync(
                It.IsAny<List<int>>(), It.IsAny<string>()), Times.Once);
        }

        // =============================
        // 20. GetAppealsByTeamAsync - Lấy appeals theo team
        // =============================
        [Test]
        public async Task GetAppealsByTeamAsync_ShouldReturnAppeals()
        {
            var appeals = new List<Appeal>
            {
                new Appeal { AppealId = 1, TeamId = 1, AppealType = AppealType.Penalty },
                new Appeal { AppealId = 2, TeamId = 1, AppealType = AppealType.Score }
            };
            var appealDtos = new List<AppealResponseDto>
            {
                new AppealResponseDto { AppealId = 1, TeamId = 1 },
                new AppealResponseDto { AppealId = 2, TeamId = 1 }
            };

            _appealRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<Appeal, bool>>>(),
                It.IsAny<Func<IQueryable<Appeal>, IOrderedQueryable<Appeal>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(appeals);

            _mapperMock.Setup(m => m.Map<IEnumerable<AppealResponseDto>>(appeals))
                      .Returns(appealDtos);

            var result = await _service.GetAppealsByTeamAsync(1);

            result.Should().HaveCount(2);
            result.First().AppealId.Should().Be(1);
        }

        // =============================
        // 21. GetAppealByIdAsync - Lấy appeal theo ID
        // =============================
        [Test]
        public async Task GetAppealByIdAsync_WhenExists_ShouldReturnAppeal()
        {
            var appeal = new Appeal { AppealId = 1, TeamId = 1 };
            var appealDto = new AppealResponseDto { AppealId = 1, TeamId = 1 };

            _appealRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<Appeal, bool>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>()))
                .ReturnsAsync(appeal);

            _mapperMock.Setup(m => m.Map<AppealResponseDto>(appeal))
                      .Returns(appealDto);

            var result = await _service.GetAppealByIdAsync(1);

            result.Should().NotBeNull();
            result.AppealId.Should().Be(1);
        }

        [Test]
        public async Task GetAppealByIdAsync_WhenNotExists_ShouldReturnNull()
        {
            _appealRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<Appeal, bool>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>(),
                It.IsAny<Expression<Func<Appeal, object>>>()))
                .ReturnsAsync((Appeal)null);

            var result = await _service.GetAppealByIdAsync(99);

            result.Should().BeNull();
        }
    }
}