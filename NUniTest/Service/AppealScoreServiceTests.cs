using AutoMapper;
using Common.DTOs.ScoreDto;
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
    public class AppealScoreServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private AppealScoreService _service;

        private Mock<IRepository<Appeal>> _appealRepo;
        private Mock<IRepository<Submission>> _submissionRepo;
        private Mock<IRepository<Score>> _scoreRepo;
        private Mock<IRepository<GroupTeam>> _groupTeamRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _appealRepo = new Mock<IRepository<Appeal>>();
            _submissionRepo = new Mock<IRepository<Submission>>();
            _scoreRepo = new Mock<IRepository<Score>>();
            _groupTeamRepo = new Mock<IRepository<GroupTeam>>();

            _uowMock.Setup(u => u.Appeals).Returns(_appealRepo.Object);
            _uowMock.Setup(u => u.Submissions).Returns(_submissionRepo.Object);
            _uowMock.Setup(u => u.Scores).Returns(_scoreRepo.Object);
            _uowMock.Setup(u => u.GroupsTeams).Returns(_groupTeamRepo.Object);

            _service = new AppealScoreService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // 1. ReScoreAppealAsync - Appeal không tồn tại
        // =============================
        [Test]
        public void ReScoreAppealAsync_WhenAppealNotFound_Throws()
        {
            var request = new FinalScoreRequestDto();

            _appealRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Appeal)null);

            Func<Task> act = async () => await _service.ReScoreAppealAsync(99, request, 10);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Appeal not found.");
        }

        // =============================
        // 2. ReScoreAppealAsync - Appeal chưa được approve
        // =============================
        [Test]
        public void ReScoreAppealAsync_WhenAppealNotApproved_Throws()
        {
            var request = new FinalScoreRequestDto();
            var appeal = new Appeal { AppealId = 1, Status = "Pending", AppealType = "Score" };

            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);

            Func<Task> act = async () => await _service.ReScoreAppealAsync(1, request, 10);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Only approved score appeals can be re-scored.");
        }

        // =============================
        // 3. ReScoreAppealAsync - Appeal không phải loại Score
        // =============================
        [Test]
        public void ReScoreAppealAsync_WhenAppealNotScoreType_Throws()
        {
            var request = new FinalScoreRequestDto();
            var appeal = new Appeal { AppealId = 1, Status = "Approved", AppealType = "Penalty" };

            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);

            Func<Task> act = async () => await _service.ReScoreAppealAsync(1, request, 10);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Only approved score appeals can be re-scored.");
        }

        // =============================
        // 4. ReScoreAppealAsync - User không phải judge của appeal
        // =============================
        [Test]
        public void ReScoreAppealAsync_WhenUserNotAuthorized_Throws()
        {
            var request = new FinalScoreRequestDto();
            var appeal = new Appeal
            {
                AppealId = 1,
                Status = "Approved",
                AppealType = "Score",
                JudgeId = 5 // Judge khác
            };

            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);

            Func<Task> act = async () => await _service.ReScoreAppealAsync(1, request, 10);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not authorized to re-score this appeal.");
        }
        // =============================
        // 5. ReScoreAppealAsync - Submission không tồn tại
        // =============================
        [Test]
        public void ReScoreAppealAsync_WhenSubmissionNotFound_Throws()
        {
            var request = new FinalScoreRequestDto();
            var appeal = new Appeal
            {
                AppealId = 1,
                Status = "Approved",
                AppealType = "Score",
                JudgeId = 10,
                SubmissionId = 99
            };

            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);

            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Submission)null);

            Func<Task> act = async () => await _service.ReScoreAppealAsync(1, request, 10);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Submission not found.");
        }

        // =============================
        // 6. ReScoreAppealAsync - Score không tồn tại cho criteria
        // =============================
        [Test]
        public void ReScoreAppealAsync_WhenScoreNotFound_Throws()
        {
            var request = new FinalScoreRequestDto
            {
                CriteriaScores = new List<CriterionScoreDto>
                {
                    new CriterionScoreDto { CriterionId = 1, Score = 9, Comment = "Excellent" }
                }
            };
            var appeal = new Appeal
            {
                AppealId = 1,
                Status = "Approved",
                AppealType = "Score",
                JudgeId = 10,
                SubmissionId = 99
            };
            var submission = new Submission { SubmissionId = 99, TeamId = 1 };

            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);

            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(submission);

            _scoreRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Score, bool>>>()))
                     .ReturnsAsync((Score)null);

            Func<Task> act = async () => await _service.ReScoreAppealAsync(1, request, 10);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("No existing score found for CriteriaId 1.");
        }

      
    }
}