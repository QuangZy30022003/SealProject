using AutoMapper;
using Common.DTOs.ScoreDto;
using Common.DTOs.Submission;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Servicefolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NUniTest.Service
{
    [TestFixture]
    public class ScoreServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private ScoreService _service;

        private Mock<IRepository<Submission>> _submissionsRepo;
        private Mock<IRepository<Score>> _scoresRepo;
        private Mock<IRepository<GroupTeam>> _groupTeamsRepo;
        private Mock<IRepository<HackathonPhase>> _phasesRepo;
        private Mock<IRepository<Ranking>> _rankingsRepo;
        private Mock<IRepository<Criterion>> _criteriaRepo;
        private Mock<IRepository<PenaltiesBonuse>> _penaltiesRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _submissionsRepo = new Mock<IRepository<Submission>>();
            _scoresRepo = new Mock<IRepository<Score>>();
            _groupTeamsRepo = new Mock<IRepository<GroupTeam>>();
            _phasesRepo = new Mock<IRepository<HackathonPhase>>();
            _rankingsRepo = new Mock<IRepository<Ranking>>();
            _criteriaRepo = new Mock<IRepository<Criterion>>();
            _penaltiesRepo = new Mock<IRepository<PenaltiesBonuse>>();

            _uowMock.Setup(u => u.Submissions).Returns(_submissionsRepo.Object);
            _uowMock.Setup(u => u.Scores).Returns(_scoresRepo.Object);
            _uowMock.Setup(u => u.GroupsTeams).Returns(_groupTeamsRepo.Object);
            _uowMock.Setup(u => u.HackathonPhases).Returns(_phasesRepo.Object);
            _uowMock.Setup(u => u.Rankings).Returns(_rankingsRepo.Object);
            _uowMock.Setup(u => u.Criteria).Returns(_criteriaRepo.Object);
            _uowMock.Setup(u => u.PenaltiesBonuses).Returns(_penaltiesRepo.Object);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _service = new ScoreService(_uowMock.Object, _mapperMock.Object);
        }

        [Test]
        public async Task UpdateAverageAndRankAsync_WhenSubmissionExists_ShouldUpdateAverageScore()
        {
            // Arrange
            var submissionId = 1;
            var submission = new Submission { SubmissionId = submissionId, TeamId = 10, PhaseId = 1 };
            var groupTeam = new GroupTeam { TeamId = 10, GroupId = 1, AverageScore = 0m, Rank = 1 };
            var scores = new List<Score>
            {
                new Score { SubmissionId = submissionId, JudgeId = 1, Score1 = 80, Criteria = new Criterion { Weight = 50 } },
                new Score { SubmissionId = submissionId, JudgeId = 2, Score1 = 90, Criteria = new Criterion { Weight = 50 } }
            };

            _submissionsRepo.Setup(r => r.GetByIdAsync(submissionId)).ReturnsAsync(submission);
            _groupTeamsRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupTeam, bool>>>()))
                .ReturnsAsync(groupTeam);
            _submissionsRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Submission, bool>>>(), null, null))
                .ReturnsAsync(new List<Submission> { submission });
            _scoresRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Score, bool>>>(), null, null))
                .ReturnsAsync(scores);
            _penaltiesRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<PenaltiesBonuse, bool>>>(), null, null))
                .ReturnsAsync(new List<PenaltiesBonuse>());
            _groupTeamsRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<GroupTeam, bool>>>(), null, null))
                .ReturnsAsync(new List<GroupTeam> { groupTeam });

            // Act
            await _service.UpdateAverageAndRankAsync(submissionId);

            // Assert
            _groupTeamsRepo.Verify(r => r.Update(It.IsAny<GroupTeam>()), Times.AtLeastOnce);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task UpdateAverageAndRankAsync_WhenSubmissionNotFound_ShouldReturn()
        {
            // Arrange
            var submissionId = 999;
            _submissionsRepo.Setup(r => r.GetByIdAsync(submissionId)).ReturnsAsync((Submission)null);

            // Act
            await _service.UpdateAverageAndRankAsync(submissionId);

            // Assert - should not throw, just return
            _groupTeamsRepo.Verify(r => r.Update(It.IsAny<GroupTeam>()), Times.Never);
        }

        [Test]
        public async Task GetTeamScoresByGroupAsync_ShouldReturnTeamScoresSorted()
        {
            // Arrange
            var groupId = 1;
            var groupTeams = new List<GroupTeam>
            {
                new GroupTeam { TeamId = 1, GroupId = groupId, AverageScore = 85m, Rank = 1, Team = new Team { TeamName = "Team A" } },
                new GroupTeam { TeamId = 2, GroupId = groupId, AverageScore = 75m, Rank = 2, Team = new Team { TeamName = "Team B" } }
            };

            _groupTeamsRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<GroupTeam, bool>>>(),
                It.IsAny<Expression<Func<GroupTeam, object>>>()))
                .ReturnsAsync(groupTeams);

            // Act
            var result = await _service.GetTeamScoresByGroupAsync(groupId);

            // Assert
            result.Should().HaveCount(2);
            result.First().AverageScore.Should().Be(85m);
            result.First().TeamName.Should().Be("Team A");
        }

        [Test]
        public void GetTeamScoresByGroupAsync_WhenNoTeamsFound_Throws()
        {
            // Arrange
            var groupId = 999;
            _groupTeamsRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<GroupTeam, bool>>>(),
                It.IsAny<Expression<Func<GroupTeam, object>>>()))
                .ReturnsAsync(new List<GroupTeam>());

            // Act & Assert
            Func<Task> act = async () => await _service.GetTeamScoresByGroupAsync(groupId);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("No teams found for this group.");
        }

        [Test]
        public async Task GetMyScoresGroupedBySubmissionAsync_ShouldReturnGroupedScores()
        {
            // Arrange
            var judgeId = 1;
            var phaseId = 1;
            var scores = new List<Score>
            {
                new Score { SubmissionId = 10, JudgeId = judgeId, Score1 = 8, Submission = new Submission { Title = "Project A", PhaseId = phaseId }, Criteria = new Criterion() },
                new Score { SubmissionId = 10, JudgeId = judgeId, Score1 = 7, Submission = new Submission { Title = "Project A", PhaseId = phaseId }, Criteria = new Criterion() },
                new Score { SubmissionId = 11, JudgeId = judgeId, Score1 = 9, Submission = new Submission { Title = "Project B", PhaseId = phaseId }, Criteria = new Criterion() }
            };

            _scoresRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Score, bool>>>(),
                It.IsAny<Expression<Func<Score, object>>>(),
                It.IsAny<Expression<Func<Score, object>>>()))
                .ReturnsAsync(scores);
            _mapperMock.Setup(m => m.Map<List<ScoreResponseDto>>(It.IsAny<List<Score>>()))
                .Returns(new List<ScoreResponseDto>());

            // Act
            var result = await _service.GetMyScoresGroupedBySubmissionAsync(judgeId, phaseId);

            // Assert
            result.Should().HaveCount(2);
            result.First().SubmissionName.Should().Be("Project A");
            result.First().TotalScore.Should().Be(15); // 8 + 7
        }

        [Test]
        public async Task GetMyScoresGroupedBySubmissionAsync_WhenNoScores_ShouldReturnEmpty()
        {
            // Arrange
            var judgeId = 999;
            var phaseId = 1;
            _scoresRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Score, bool>>>(),
                It.IsAny<Expression<Func<Score, object>>>(),
                It.IsAny<Expression<Func<Score, object>>>()))
                .ReturnsAsync(new List<Score>());

            // Act
            var result = await _service.GetMyScoresGroupedBySubmissionAsync(judgeId, phaseId);

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public void ScoreSubmissionAsync_WhenSubmissionNotFound_Throws()
        {
            // Arrange
            var request = new ScoreSubmissionRequestDto
            {
                SubmissionId = 999,
                CriteriaScores = new List<CriterionScoreDto> { new CriterionScoreDto { CriterionId = 1, Score = 8 } }
            };

            _submissionsRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Submission)null);

            // Act & Assert
            Func<Task> act = async () => await _service.ScoreSubmissionAsync(1, request);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("Submission not found");
        }

        [Test]
        public void ScoreSubmissionAsync_WhenNoCriteriaScores_Throws()
        {
            // Arrange
            var request = new ScoreSubmissionRequestDto
            {
                SubmissionId = 1,
                CriteriaScores = null
            };

            // Act & Assert
            Func<Task> act = async () => await _service.ScoreSubmissionAsync(1, request);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("No scores provided.");
        }

        [Test]
        public async Task UpdateScoreByIdAsync_WhenScoreExists_ShouldUpdateSuccessfully()
        {
            // Arrange
            var scoreId = 1;
            var judgeId = 1;
            var score = new Score { ScoreId = scoreId, JudgeId = judgeId, SubmissionId = 10, Score1 = 5 };
            var submission = new Submission { SubmissionId = 10, PhaseId = 1, TeamId = 100 };
            var criterion = new Criterion { CriteriaId = 1, Weight = 10 };
            var request = new ScoreUpdateByIdDto { ScoreValue = 8, Comment = "Updated" };

            _scoresRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Score, bool>>>()))
                .ReturnsAsync(score);
            _criteriaRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Criterion, bool>>>()))
                .ReturnsAsync(criterion);
            _submissionsRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(submission);
            _groupTeamsRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<GroupTeam, bool>>>()))
                .ReturnsAsync(new GroupTeam { TeamId = 100, GroupId = 1 });
            _submissionsRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Submission, bool>>>(), null, null))
                .ReturnsAsync(new List<Submission> { submission });
            _scoresRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Score, bool>>>(), null, null))
                .ReturnsAsync(new List<Score>());
            _groupTeamsRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<GroupTeam, bool>>>(), null, null))
                .ReturnsAsync(new List<GroupTeam> { new GroupTeam { GroupId = 1, TeamId = 100 } });
            _mapperMock.Setup(m => m.Map<ScoreDetailDto>(score))
                .Returns(new ScoreDetailDto { ScoreId = scoreId, ScoreValue = 8 });

            // Act
            var result = await _service.UpdateScoreByIdAsync(judgeId, scoreId, request);

            // Assert
            result.Should().NotBeNull();
            score.Score1.Should().Be(8);
            _scoresRepo.Verify(r => r.Update(score), Times.Once);
        }

        [Test]
        public void UpdateScoreByIdAsync_WhenScoreNotFound_Throws()
        {
            // Arrange
            var scoreId = 999;
            var request = new ScoreUpdateByIdDto { ScoreValue = 8 };

            _scoresRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Score, bool>>>()))
                .ReturnsAsync((Score)null);

            // Act & Assert
            Func<Task> act = async () => await _service.UpdateScoreByIdAsync(1, scoreId, request);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("Score not found");
        }

        [Test]
        public void UpdateScoreByIdAsync_WhenJudgeNotAuthorized_Throws()
        {
            // Arrange
            var scoreId = 1;
            var judgeId = 1;
            var score = new Score { ScoreId = scoreId, JudgeId = 2, SubmissionId = 10, Score1 = 5 };
            var request = new ScoreUpdateByIdDto { ScoreValue = 8 };

            _scoresRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Score, bool>>>()))
                .ReturnsAsync(score);

            // Act & Assert
            Func<Task> act = async () => await _service.UpdateScoreByIdAsync(judgeId, scoreId, request);
            act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not allowed to update this score");
        }

        [Test]
        public async Task GetTeamOverviewAsync_ShouldReturnTeamOverviewWithJudges()
        {
            // Arrange
            var teamId = 1;
            var phaseId = 1;
            var team = new Team { TeamId = teamId, TeamName = "Team A" };
            var groupTeam = new GroupTeam { TeamId = teamId, GroupId = 1, AverageScore = 85m, Rank = 1, Group = new Group { TrackId = 1 } };
            var scores = new List<Score>
            {
                new Score { SubmissionId = 10, JudgeId = 1, Score1 = 8, Criteria = new Criterion { PhaseId = phaseId }, Submission = new Submission { Title = "Sub 1" }, Judge = new User { FullName = "Judge 1" } }
            };

            _uowMock.Setup(u => u.Teams).Returns(new Mock<IRepository<Team>>().Object);
            var teamsRepo = new Mock<IRepository<Team>>();
            _uowMock.Setup(u => u.Teams).Returns(teamsRepo.Object);
            teamsRepo.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);

            _groupTeamsRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<GroupTeam, bool>>>(),
                It.IsAny<Expression<Func<GroupTeam, object>>>(),
                It.IsAny<Expression<Func<GroupTeam, object>>>()))
                .ReturnsAsync(new List<GroupTeam> { groupTeam });

            _scoresRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Score, bool>>>(),
                It.IsAny<Expression<Func<Score, object>>>(),
                It.IsAny<Expression<Func<Score, object>>>()))
                .ReturnsAsync(scores);

            // Act
            var result = await _service.GetTeamOverviewAsync(teamId, phaseId);

            // Assert
            result.Should().NotBeNull();
            result.TeamId.Should().Be(teamId);
            result.TeamName.Should().Be("Team A");
            result.AverageScore.Should().Be(85m);
        }

        [Test]
        public void GetTeamOverviewAsync_WhenTeamNotFound_Throws()
        {
            // Arrange
            var teamId = 999;
            var phaseId = 1;
            var teamsRepo = new Mock<IRepository<Team>>();
            _uowMock.Setup(u => u.Teams).Returns(teamsRepo.Object);
            teamsRepo.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync((Team)null);

            // Act & Assert
            Func<Task> act = async () => await _service.GetTeamOverviewAsync(teamId, phaseId);
            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Team not found");
        }

        [Test]
        public async Task UpdateFinalRankingAsync_ShouldUpdateRankingsCorrectly()
        {
            // Arrange
            var submission = new Submission { SubmissionId = 1, TeamId = 10, PhaseId = 1 };
            var hackathonId = 1;
            var scores = new List<Score>
            {
                new Score { SubmissionId = 1, JudgeId = 1, Score1 = 80, Criteria = new Criterion { Weight = 50 } }
            };
            var ranking = new Ranking { TeamId = 10, HackathonId = hackathonId, TotalScore = 40m, Rank = 1 };

            _scoresRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Score, bool>>>(), null, null))
                .ReturnsAsync(scores);
            _penaltiesRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<PenaltiesBonuse, bool>>>(), null, null))
                .ReturnsAsync(new List<PenaltiesBonuse>());
            _rankingsRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Ranking, bool>>>()))
                .ReturnsAsync((Ranking)null);
            _rankingsRepo.Setup(r => r.AddAsync(It.IsAny<Ranking>())).Returns(Task.CompletedTask);
            _rankingsRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<Ranking, bool>>>(),
                It.IsAny<Func<IQueryable<Ranking>, IOrderedQueryable<Ranking>>>(),
                null))
                .ReturnsAsync(new List<Ranking> { ranking });

            // Act
            await _service.UpdateFinalRankingAsync(submission, hackathonId);

            // Assert
            _rankingsRepo.Verify(r => r.AddAsync(It.IsAny<Ranking>()), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce);
        }
    }
}
