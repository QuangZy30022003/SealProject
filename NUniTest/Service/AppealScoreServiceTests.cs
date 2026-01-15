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

        // =============================
        // 7. ReScoreAppealAsync - Re-score thành công
        // =============================
        [Test]
        public async Task ReScoreAppealAsync_WhenValid_ShouldUpdateScores()
        {
            var request = new FinalScoreRequestDto
            {
                CriteriaScores = new List<CriterionScoreDto>
        {
            new CriterionScoreDto { CriterionId = 1, Score = 9, Comment = "Excellent after review" },
            new CriterionScoreDto { CriterionId = 2, Score = 8, Comment = "Good improvement" }
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

            var existingScores = new List<Score>
    {
        new Score { ScoreId = 1, SubmissionId = 99, JudgeId = 10, CriteriaId = 1, Score1 = 7, Comment = "Good" },
        new Score { ScoreId = 2, SubmissionId = 99, JudgeId = 10, CriteriaId = 2, Score1 = 6, Comment = "Average" }
    };

            // ============ MOCK APPEAL & SUBMISSION ============
            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);
            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(submission);

            // ============ MOCK SCORE TÌM THEO CRITERIA ============
            _scoreRepo.SetupSequence(r =>
                    r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Score, bool>>>()))
                .ReturnsAsync(existingScores[0])
                .ReturnsAsync(existingScores[1]);

            _scoreRepo.Setup(r => r.Update(It.IsAny<Score>()));

            // ============ MOCK CHO UpdateAverageAndRankAsync ============
            _scoreRepo.Setup(r =>
                    r.GetAllAsync(It.IsAny<Expression<Func<Score, bool>>>(), null, null))
                .ReturnsAsync(existingScores);

            _groupTeamRepo.Setup(r =>
                    r.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupTeam, bool>>>()))
                .ReturnsAsync(new GroupTeam
                {
                    TeamId = 1,
                    GroupId = 10,
                    AverageScore = 0
                });

            _groupTeamRepo.Setup(r => r.Update(It.IsAny<GroupTeam>()));

            _groupTeamRepo.Setup(r =>
                    r.GetAllAsync(It.IsAny<Expression<Func<GroupTeam, bool>>>(), null, null))
                .ReturnsAsync(new List<GroupTeam>
                {
            new GroupTeam { TeamId = 1, GroupId = 10, AverageScore = 0 }
                });

            _submissionRepo.Setup(r =>
                    r.GetAllAsync(It.IsAny<Expression<Func<Submission, bool>>>(), null, null))
                .ReturnsAsync(new List<Submission> { submission });

            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            // ============ MOCK MAPPER ============
            _mapperMock.SetupSequence(m =>
                    m.Map<ScoreItemDto>(It.IsAny<CriterionScoreDto>()))
                .Returns(new ScoreItemDto { CriteriaId = 1, ScoreValue = 9, Comment = "Excellent after review" })
                .Returns(new ScoreItemDto { CriteriaId = 2, ScoreValue = 8, Comment = "Good improvement" });

            // ============ CALL SERVICE ============
            var result = await _service.ReScoreAppealAsync(1, request, 10);

            // ============ VERIFY ============
            result.SubmissionId.Should().Be(99);
            result.Scores.Should().HaveCount(2);

            existingScores[0].Score1.Should().Be(9);
            existingScores[0].Comment.Should().Be("Excellent after review");

            existingScores[1].Score1.Should().Be(8);
            existingScores[1].Comment.Should().Be("Good improvement");

            _scoreRepo.Verify(r => r.Update(It.IsAny<Score>()), Times.Exactly(2));
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }

        // =============================
        // 8. ReScoreAppealAsync - Re-score với multiple criteria
        // =============================
        [Test]
        public async Task ReScoreAppealAsync_WhenMultipleCriteria_ShouldUpdateAllScores()
        {
            // ===== INPUT =====
            var request = new FinalScoreRequestDto
            {
                CriteriaScores = new List<CriterionScoreDto>
        {
            new CriterionScoreDto { CriterionId = 1, Score = 10, Comment = "Perfect" },
            new CriterionScoreDto { CriterionId = 2, Score = 9, Comment = "Excellent" },
            new CriterionScoreDto { CriterionId = 3, Score = 8, Comment = "Very good" }
        }
            };

            // ===== MOCK DATA =====
            var appeal = new Appeal
            {
                AppealId = 1,
                Status = "Approved",
                AppealType = "Score",
                JudgeId = 10,
                SubmissionId = 99
            };

            var submission = new Submission
            {
                SubmissionId = 99,
                TeamId = 1
            };

            var existingScores = new List<Score>
    {
        new Score { ScoreId = 1, SubmissionId = 99, JudgeId = 10, CriteriaId = 1, Score1 = 5, Comment = "Poor" },
        new Score { ScoreId = 2, SubmissionId = 99, JudgeId = 10, CriteriaId = 2, Score1 = 6, Comment = "Below average" },
        new Score { ScoreId = 3, SubmissionId = 99, JudgeId = 10, CriteriaId = 3, Score1 = 7, Comment = "Average" }
    };

            // ===== MOCK APPEAL =====
            _appealRepo.Setup(r => r.GetByIdAsync(1))
                       .ReturnsAsync(appeal);

            // ===== MOCK SUBMISSION =====
            _submissionRepo.Setup(r => r.GetByIdAsync(99))
                           .ReturnsAsync(submission);

            // ===== MOCK SCORES (existing per criterion) =====
            _scoreRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Score, bool>>>()))
                      .ReturnsAsync(existingScores[0])
                      .ReturnsAsync(existingScores[1])
                      .ReturnsAsync(existingScores[2]);

            // ===== UPDATE: Scores will be updated =====
            _scoreRepo.Setup(r => r.Update(It.IsAny<Score>()));

            // ===== Needed by UpdateAverageAndRankAsync =====

            // All scores after update
            _scoreRepo.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Score, bool>>>(), null, null))
                .ReturnsAsync(existingScores);

            // GroupTeam for this submission
            _groupTeamRepo.Setup(r => r.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<GroupTeam, bool>>>()))
                .ReturnsAsync(new GroupTeam
                {
                    GroupId = 10,
                    TeamId = 1,
                    AverageScore = 0,
                    Rank = 1
                });

            _groupTeamRepo.Setup(r => r.Update(It.IsAny<GroupTeam>()));

            // Other teams in same group for ranking
            _groupTeamRepo.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<GroupTeam, bool>>>(), null, null))
                .ReturnsAsync(new List<GroupTeam>
                {
            new GroupTeam { TeamId = 1, GroupId = 10, AverageScore = 0, Rank = 1 }
                });

            _submissionRepo.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<Submission, bool>>>(), null, null))
                .ReturnsAsync(new List<Submission> { submission });

            // ===== Save =====
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            // ===== Mapper → return final DTO =====
            _mapperMock.Setup(m => m.Map<ScoreItemDto>(It.IsAny<CriterionScoreDto>()))
                       .Returns((CriterionScoreDto dto) =>
                            new ScoreItemDto
                            {
                                CriteriaId = dto.CriterionId,
                                ScoreValue = dto.Score,
                                Comment = dto.Comment
                            }
                        );

            // ===== CALL SERVICE =====
            var result = await _service.ReScoreAppealAsync(1, request, 10);

            // ===== ASSERT =====
            result.Should().NotBeNull();
            result.SubmissionId.Should().Be(99);
            result.Scores.Should().HaveCount(3);

            // Each criterion updated correctly
            existingScores[0].Score1.Should().Be(10);
            existingScores[0].Comment.Should().Be("Perfect");

            existingScores[1].Score1.Should().Be(9);
            existingScores[1].Comment.Should().Be("Excellent");

            existingScores[2].Score1.Should().Be(8);
            existingScores[2].Comment.Should().Be("Very good");

            // Three updates = three criteria
            _scoreRepo.Verify(r => r.Update(It.IsAny<Score>()), Times.Exactly(3));

            // Save called at least once
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }


        // =============================
        // 9. ReScoreAppealAsync - Verify ScoredAt timestamp được update
        // =============================
        [Test]
        public async Task ReScoreAppealAsync_WhenValid_ShouldUpdateScoredAtTimestamp()
        {
            var request = new FinalScoreRequestDto
            {
                CriteriaScores = new List<CriterionScoreDto>
        {
            new CriterionScoreDto { CriterionId = 1, Score = 9, Comment = "Updated score" }
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

            var existingScore = new Score
            {
                ScoreId = 1,
                SubmissionId = 99,
                JudgeId = 10,
                CriteriaId = 1,
                Score1 = 7,
                Comment = "Original",
                ScoredAt = DateTime.UtcNow.AddDays(-1)
            };

            var beforeUpdate = DateTime.UtcNow;

            // ============ MOCK APPEAL & SUBMISSION ============
            _appealRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(appeal);
            _submissionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(submission);

            // ============ MOCK EXISTING SCORE ============
            _scoreRepo.Setup(r =>
                    r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Score, bool>>>()))
                .ReturnsAsync(existingScore);

            _scoreRepo.Setup(r => r.Update(It.IsAny<Score>()));

            // ============ MOCK CHO UpdateAverageAndRankAsync ============
            // Tất cả score của team
            _scoreRepo.Setup(r =>
                    r.GetAllAsync(It.IsAny<Expression<Func<Score, bool>>>(), null, null))
                .ReturnsAsync(new List<Score> { existingScore });

            // GroupTeam hiện tại của team
            _groupTeamRepo.Setup(r =>
                    r.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupTeam, bool>>>()))
                .ReturnsAsync(new GroupTeam
                {
                    TeamId = 1,
                    GroupId = 10,
                    AverageScore = 0
                });

            _groupTeamRepo.Setup(r => r.Update(It.IsAny<GroupTeam>()));

            // Tất cả team trong group phục vụ tính rank
            _groupTeamRepo.Setup(r =>
                    r.GetAllAsync(It.IsAny<Expression<Func<GroupTeam, bool>>>(), null, null))
                .ReturnsAsync(new List<GroupTeam>
                {
            new GroupTeam { TeamId = 1, GroupId = 10, AverageScore = 0 }
                });

            // Tất cả submission phục vụ mapping team
            _submissionRepo.Setup(r =>
                    r.GetAllAsync(It.IsAny<Expression<Func<Submission, bool>>>(), null, null))
                .ReturnsAsync(new List<Submission> { submission });

            // Save
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            // Mapping
            _mapperMock.Setup(m => m.Map<ScoreItemDto>(It.IsAny<CriterionScoreDto>()))
                .Returns(new ScoreItemDto { CriteriaId = 1, ScoreValue = 9, Comment = "Updated score" });

            // ============ CALL SERVICE ============
            await _service.ReScoreAppealAsync(1, request, 10);

            var afterUpdate = DateTime.UtcNow;

            // ============ VERIFY ============
            existingScore.ScoredAt.Should().BeAfter(beforeUpdate);
            existingScore.ScoredAt.Should().BeBefore(afterUpdate);
        }

    }
}