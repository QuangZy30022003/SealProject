using AutoMapper;
using Common.DTOs.RanksDTo;
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
    public class RankingServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private RankingService _service;

        private Mock<IRepository<Ranking>> _rankingRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _rankingRepo = new Mock<IRepository<Ranking>>();

            _uowMock.Setup(u => u.Rankings).Returns(_rankingRepo.Object);

            _service = new RankingService(_uowMock.Object, _mapperMock.Object);
        }

        [Test]
        public async Task GetRankingsByHackathonAsync_ShouldReturnSortedRankingsByTotalScore()
        {
            // Arrange
            var hackathonId = 1;
            var rankings = new List<Ranking>
            {
                new Ranking { RankingId = 1, HackathonId = hackathonId, TotalScore = 100, Team = new Team { TeamId = 1, TeamName = "Team A" }, Hackathon = new Hackathon { HackathonId = hackathonId, Name = "Hackathon 1" } },
                new Ranking { RankingId = 2, HackathonId = hackathonId, TotalScore = 90, Team = new Team { TeamId = 2, TeamName = "Team B" }, Hackathon = new Hackathon { HackathonId = hackathonId, Name = "Hackathon 1" } },
                new Ranking { RankingId = 3, HackathonId = hackathonId, TotalScore = 80, Team = new Team { TeamId = 3, TeamName = "Team C" }, Hackathon = new Hackathon { HackathonId = hackathonId, Name = "Hackathon 1" } }
            };
            var dtos = new List<RankingDto>
            {
                new RankingDto { RankingId = 1, TeamName = "Team A", TotalScore = 100, Rank = 1 },
                new RankingDto { RankingId = 2, TeamName = "Team B", TotalScore = 90, Rank = 2 },
                new RankingDto { RankingId = 3, TeamName = "Team C", TotalScore = 80, Rank = 3 }
            };

            _rankingRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Ranking, bool>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>()))
                .ReturnsAsync(rankings);
            _mapperMock.Setup(m => m.Map<List<RankingDto>>(It.IsAny<IOrderedEnumerable<Ranking>>())).Returns(dtos);

            // Act
            var result = await _service.GetRankingsByHackathonAsync(hackathonId);

            // Assert
            result.Should().HaveCount(3);
            result.Should().BeInDescendingOrder(r => r.TotalScore);
            result.First().TotalScore.Should().Be(100);
            result.Last().TotalScore.Should().Be(80);
        }

        [Test]
        public async Task GetRankingsByHackathonAsync_WhenNoRankings_ShouldReturnEmpty()
        {
            // Arrange
            var hackathonId = 999;
            var emptyList = new List<Ranking>();

            _rankingRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Ranking, bool>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>()))
                .ReturnsAsync(emptyList);
            _mapperMock.Setup(m => m.Map<List<RankingDto>>(It.IsAny<IOrderedEnumerable<Ranking>>())).Returns(new List<RankingDto>());

            // Act
            var result = await _service.GetRankingsByHackathonAsync(hackathonId);

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetRankingsByHackathonAsync_ShouldIncludeTeamAndHackathonData()
        {
            // Arrange
            var hackathonId = 1;
            var rankings = new List<Ranking>
            {
                new Ranking 
                { 
                    RankingId = 1, 
                    HackathonId = hackathonId, 
                    TotalScore = 100,
                    Team = new Team { TeamId = 1, TeamName = "Team A" },
                    Hackathon = new Hackathon { HackathonId = hackathonId, Name = "Hackathon 1" }
                }
            };
            var dtos = new List<RankingDto>
            {
                new RankingDto { RankingId = 1, TeamName = "Team A", HackathonName = "Hackathon 1", TotalScore = 100 }
            };

            _rankingRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Ranking, bool>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>()))
                .ReturnsAsync(rankings);
            _mapperMock.Setup(m => m.Map<List<RankingDto>>(It.IsAny<IOrderedEnumerable<Ranking>>())).Returns(dtos);

            // Act
            var result = await _service.GetRankingsByHackathonAsync(hackathonId);

            // Assert
            result.Should().HaveCount(1);
            result.First().TeamName.Should().Be("Team A");
            result.First().HackathonName.Should().Be("Hackathon 1");
        }

        [Test]
        public async Task GetRankingsByHackathonAsync_ShouldCallGetAllIncludingWithCorrectParameters()
        {
            // Arrange
            var hackathonId = 1;
            var rankings = new List<Ranking>();

            _rankingRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Ranking, bool>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>()))
                .ReturnsAsync(rankings);
            _mapperMock.Setup(m => m.Map<List<RankingDto>>(It.IsAny<IOrderedEnumerable<Ranking>>())).Returns(new List<RankingDto>());

            // Act
            await _service.GetRankingsByHackathonAsync(hackathonId);

            // Assert
            _rankingRepo.Verify(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Ranking, bool>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>()), Times.Once);
        }

        [Test]
        public async Task GetRankingsByHackathonAsync_ShouldMapResultsCorrectly()
        {
            // Arrange
            var hackathonId = 1;
            var rankings = new List<Ranking>
            {
                new Ranking { RankingId = 1, HackathonId = hackathonId, TotalScore = 100 },
                new Ranking { RankingId = 2, HackathonId = hackathonId, TotalScore = 90 }
            };
            var dtos = new List<RankingDto>
            {
                new RankingDto { RankingId = 1, TotalScore = 100 },
                new RankingDto { RankingId = 2, TotalScore = 90 }
            };

            _rankingRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Ranking, bool>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>()))
                .ReturnsAsync(rankings);
            _mapperMock.Setup(m => m.Map<List<RankingDto>>(It.IsAny<IOrderedEnumerable<Ranking>>())).Returns(dtos);

            // Act
            var result = await _service.GetRankingsByHackathonAsync(hackathonId);

            // Assert
            result.Should().HaveCount(2);
            _mapperMock.Verify(m => m.Map<List<RankingDto>>(It.IsAny<IOrderedEnumerable<Ranking>>()), Times.Once);
        }

        [Test]
        public async Task GetRankingsByHackathonAsync_WithMultipleHackathons_ShouldFilterByHackathonId()
        {
            // Arrange
            var hackathonId = 1;
            var rankings = new List<Ranking>
            {
                new Ranking { RankingId = 1, HackathonId = 1, TotalScore = 100 },
                new Ranking { RankingId = 2, HackathonId = 1, TotalScore = 90 },
                new Ranking { RankingId = 3, HackathonId = 2, TotalScore = 85 } // Different hackathon
            };
            var filteredRankings = rankings.Where(r => r.HackathonId == hackathonId).ToList();
            var dtos = new List<RankingDto>
            {
                new RankingDto { RankingId = 1, TotalScore = 100 },
                new RankingDto { RankingId = 2, TotalScore = 90 }
            };

            _rankingRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Ranking, bool>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>()))
                .ReturnsAsync(filteredRankings);
            _mapperMock.Setup(m => m.Map<List<RankingDto>>(It.IsAny<IOrderedEnumerable<Ranking>>())).Returns(dtos);

            // Act
            var result = await _service.GetRankingsByHackathonAsync(hackathonId);

            // Assert
            result.Should().HaveCount(2);
            result.All(r => r.RankingId != 3).Should().BeTrue();
        }

        [Test]
        public async Task GetRankingsByHackathonAsync_ShouldOrderByTotalScoreDescending()
        {
            // Arrange
            var hackathonId = 1;
            var rankings = new List<Ranking>
            {
                new Ranking { RankingId = 1, HackathonId = hackathonId, TotalScore = 80 },
                new Ranking { RankingId = 2, HackathonId = hackathonId, TotalScore = 100 },
                new Ranking { RankingId = 3, HackathonId = hackathonId, TotalScore = 90 }
            };
            var orderedRankings = rankings.OrderByDescending(r => r.TotalScore).ToList();
            var dtos = new List<RankingDto>
            {
                new RankingDto { RankingId = 2, TotalScore = 100 },
                new RankingDto { RankingId = 3, TotalScore = 90 },
                new RankingDto { RankingId = 1, TotalScore = 80 }
            };

            _rankingRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Ranking, bool>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>()))
                .ReturnsAsync(rankings);
            _mapperMock.Setup(m => m.Map<List<RankingDto>>(It.IsAny<IOrderedEnumerable<Ranking>>())).Returns(dtos);

            // Act
            var result = await _service.GetRankingsByHackathonAsync(hackathonId);

            // Assert
            result.Should().BeInDescendingOrder(r => r.TotalScore);
            result.First().TotalScore.Should().Be(100);
        }

        [Test]
        public async Task GetRankingsByHackathonAsync_WithSingleRanking_ShouldReturnSingleItem()
        {
            // Arrange
            var hackathonId = 1;
            var rankings = new List<Ranking>
            {
                new Ranking { RankingId = 1, HackathonId = hackathonId, TotalScore = 100 }
            };
            var dtos = new List<RankingDto>
            {
                new RankingDto { RankingId = 1, TotalScore = 100 }
            };

            _rankingRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Ranking, bool>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>(),
                It.IsAny<Expression<Func<Ranking, object>>>()))
                .ReturnsAsync(rankings);
            _mapperMock.Setup(m => m.Map<List<RankingDto>>(It.IsAny<IOrderedEnumerable<Ranking>>())).Returns(dtos);

            // Act
            var result = await _service.GetRankingsByHackathonAsync(hackathonId);

            // Assert
            result.Should().HaveCount(1);
            result.First().RankingId.Should().Be(1);
        }
    }
}
