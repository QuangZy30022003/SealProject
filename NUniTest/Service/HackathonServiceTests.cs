using AutoMapper;
using Common.DTOs.HackathonDto;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Servicefolder;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NUniTest.Service
{
    [TestFixture]
    public class HackathonServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;

        private Mock<IRepository<Hackathon>> _hackathonRepo;
        private Mock<IRepository<Season>> _seasonRepo;
        private Mock<IRepository<HackathonPhase>> _phaseRepo;

        private Mock<IHackathonPhaseRepository> _hackathonPhaseRepo;

        private HackathonService _service;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _hackathonRepo = new Mock<IRepository<Hackathon>>();
            _seasonRepo = new Mock<IRepository<Season>>();
            _phaseRepo = new Mock<IRepository<HackathonPhase>>();
            _hackathonPhaseRepo = new Mock<IHackathonPhaseRepository>();

            _uowMock.Setup(u => u.Hackathons).Returns(_hackathonRepo.Object);
            _uowMock.Setup(u => u.Seasons).Returns(_seasonRepo.Object);
            _uowMock.Setup(u => u.HackathonPhases).Returns(_phaseRepo.Object);
            _uowMock.Setup(u => u.HackathonPhaseRepository).Returns(_hackathonPhaseRepo.Object);

            _service = new HackathonService(_uowMock.Object, _mapperMock.Object);
        }

        // ============================================================
        // 1. CREATE HACKATHON
        // ============================================================

        [Test]
        public void CreateHackathonAsync_WhenSeasonNotFound_Throws()
        {
            var dto = new HackathonCreateDto
            {
                SeasonId = 10,
                Name = "Test"
            };

            _seasonRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Season)null);

            Func<Task> act = async () => await _service.CreateHackathonAsync(dto, 1);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Season not found");
        }

        [Test]
        public void CreateHackathonAsync_WhenEndDateInvalid_Throws()
        {
            var season = new Season
            {
                SeasonId = 1,
                StartDate = DateTime.Parse("2024-01-01"),
                EndDate = DateTime.Parse("2024-12-31")
            };

            var dto = new HackathonCreateDto
            {
                SeasonId = 1,
                StartDate = new DateOnly(2024, 05, 01),
                EndDate = new DateOnly(2024, 04, 01)
            };

            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);

            Func<Task> act = async () => await _service.CreateHackathonAsync(dto, 1);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("EndDate must be later than StartDate");
        }

        [Test]
        public void CreateHackathonAsync_WhenDateOutsideSeason_Throws()
        {
            var season = new Season
            {
                StartDate = DateTime.Parse("2024-01-01"),
                EndDate = DateTime.Parse("2024-06-30")
            };

            var dto = new HackathonCreateDto
            {
                SeasonId = 1,
                StartDate = new DateOnly(2024, 05, 01),
                EndDate = new DateOnly(2024, 10, 01)
            };

            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);

            Func<Task> act = async () => await _service.CreateHackathonAsync(dto, 1);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Hackathon dates must fall within the season (2024-01-01 - 2024-06-30)");
        }

        [Test]
        public async Task CreateHackathonAsync_WhenValid_ShouldCreate()
        {
            var season = new Season
            {
                SeasonId = 1,
                StartDate = DateTime.Parse("2024-01-01"),
                EndDate = DateTime.Parse("2024-12-31")
            };

            var dto = new HackathonCreateDto
            {
                SeasonId = 1,
                Name = "Hack 2024",
                Description = "Desc",
                StartDate = new DateOnly(2024, 05, 01),
                EndDate = new DateOnly(2024, 05, 10)
            };

            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);

            _hackathonRepo.Setup(r => r.AddAsync(It.IsAny<Hackathon>()))
                .Returns(Task.CompletedTask);

            _uowMock.Setup(u => u.SaveAsync(null)).ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<HackathonResponseDto>(It.IsAny<Hackathon>()))
                .Returns(new HackathonResponseDto { Name = "Hack 2024" });

            var result = await _service.CreateHackathonAsync(dto, 9);

            result.Name.Should().Be("Hack 2024");

            _hackathonRepo.Verify(r => r.AddAsync(It.IsAny<Hackathon>()), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(null), Times.Once);
        }

        // ============================================================
        // 2. UPDATE HACKATHON
        // ============================================================

        [Test]
        public void UpdateHackathonAsync_WhenNotFound_ReturnsNull()
        {
            _hackathonRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((Hackathon)null);

            var dto = new HackathonCreateDto();

            var result = _service.UpdateHackathonAsync(5, dto, 1).Result;

            result.Should().BeNull();
        }

        [Test]
        public void UpdateHackathonAsync_WhenUnauthorized_Throws()
        {
            var hackathon = new Hackathon { HackathonId = 5, CreatedBy = 9 };

            _hackathonRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(hackathon);

            var dto = new HackathonCreateDto();

            Func<Task> act = async () => await _service.UpdateHackathonAsync(5, dto, 2);

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not authorized to update this hackathon");
        }

        [Test]
        public void UpdateHackathonAsync_WhenSeasonNotFound_Throws()
        {
            var hackathon = new Hackathon { HackathonId = 5, CreatedBy = 1 };

            _hackathonRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(hackathon);
            _seasonRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync((Season)null);

            var dto = new HackathonCreateDto { SeasonId = 3 };

            Func<Task> act = async () => await _service.UpdateHackathonAsync(5, dto, 1);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Season not found");
        }

        [Test]
        public void UpdateHackathonAsync_WhenInvalidDates_Throws()
        {
            var season = new Season
            {
                StartDate = DateTime.Parse("2024-01-01"),
                EndDate = DateTime.Parse("2024-12-31")
            };

            var hackathon = new Hackathon { CreatedBy = 10 };

            _hackathonRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(hackathon);
            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);

            var dto = new HackathonCreateDto
            {
                SeasonId = 1,
                StartDate = new DateOnly(2024, 05, 02),
                EndDate = new DateOnly(2024, 05, 01)
            };

            Func<Task> act = async () => await _service.UpdateHackathonAsync(5, dto, 10);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("EndDate must be later than StartDate");
        }

        [Test]
        public void UpdateHackathonAsync_WhenPhaseOutsideRange_Throws()
        {
            var season = new Season
            {
                StartDate = DateTime.Parse("2024-01-01"),
                EndDate = DateTime.Parse("2024-12-31")
            };

            var hackathon = new Hackathon
            {
                CreatedBy = 9,
                StartDate = new DateOnly(2024, 03, 01),
                EndDate = new DateOnly(2024, 03, 10)
            };

            var phases = new List<HackathonPhase>
            {
                new HackathonPhase { StartDate = DateTime.Parse("2024-03-02"), EndDate = DateTime.Parse("2024-03-09") }
            };

            _hackathonRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(hackathon);
            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);
            _phaseRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<HackathonPhase, bool>>>(),
                It.IsAny<Func<IQueryable<HackathonPhase>, IOrderedQueryable<HackathonPhase>>>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(phases);


            var dto = new HackathonCreateDto
            {
                SeasonId = 1,
                StartDate = new DateOnly(2024, 03, 05), // Lớn hơn min phase
                EndDate = new DateOnly(2024, 03, 06)   // Nhỏ hơn max phase
            };

            Func<Task> act = async () => await _service.UpdateHackathonAsync(5, dto, 9);

            act.Should().ThrowAsync<ArgumentException>();
        }

        [Test]
        public async Task UpdateHackathonAsync_WhenValid_ShouldUpdate()
        {
            // Arrange
            var season = new Season
            {
                StartDate = DateTime.Parse("2024-01-01"),
                EndDate = DateTime.Parse("2024-12-31")
            };

            var hackathon = new Hackathon
            {
                HackathonId = 5,
                CreatedBy = 2,
                Name = "Old"
            };

            // ADD THIS ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓
            var phases = new List<HackathonPhase>();
            // or create sample phases depending on test case

            _hackathonRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(hackathon);
            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);

            _phaseRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<HackathonPhase, bool>>>(),
                It.IsAny<Func<IQueryable<HackathonPhase>, IOrderedQueryable<HackathonPhase>>>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(phases);

            _uowMock.Setup(u => u.SaveAsync(null)).ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<HackathonResponseDto>(hackathon))
                .Returns(new HackathonResponseDto { Name = "New" });

            var dto = new HackathonCreateDto
            {
                SeasonId = 1,
                Name = "New",
                Description = "Desc",
                StartDate = new DateOnly(2024, 05, 01),
                EndDate = new DateOnly(2024, 05, 10)
            };

            // Act
            var result = await _service.UpdateHackathonAsync(5, dto, 2);

            // Assert
            result.Name.Should().Be("New");

            _hackathonRepo.Verify(r => r.Update(hackathon), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(null), Times.Once);
        }


        // ============================================================
        // 3. DELETE
        // ============================================================

        [Test]
        public void DeleteAsync_WhenStartDatePassed_Throws()
        {
            var hackathon = new Hackathon
            {
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
            };

            _hackathonRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(hackathon);

            Func<Task> act = async () => await _service.DeleteAsync(3);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot delete a hackathon that has already started or ended.");
        }

        [Test]
        public async Task DeleteAsync_WhenValid_ShouldDelete()
        {
            var hackathon = new Hackathon
            {
                HackathonId = 3,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5))
            };

            _hackathonRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(hackathon);

            _uowMock.Setup(u => u.SaveAsync(null)).ReturnsAsync(1);

            var ok = await _service.DeleteAsync(3);

            ok.Should().BeTrue();

            _hackathonRepo.Verify(r => r.Remove(hackathon), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(null), Times.Once);
        }

        // ============================================================
        // 4. UPDATE STATUS
        // ============================================================

        [Test]
        public void UpdateStatusAsync_WhenInvalidStatus_Throws()
        {
            Func<Task> act = async () => await _service.UpdateStatusAsync(5, "Wrong");

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Invalid status");
        }

        [Test]
        public async Task UpdateStatusAsync_WhenValid_ShouldUpdate()
        {
            var hackathon = new Hackathon { HackathonId = 5, Status = "Pending" };

            _hackathonRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(hackathon);
            _uowMock.Setup(u => u.SaveAsync(null)).ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<HackathonResponseDto>(hackathon))
                .Returns(new HackathonResponseDto { Status = "Complete" });

            var result = await _service.UpdateStatusAsync(5, "Complete");

            result.Status.Should().Be("Complete");

            _hackathonRepo.Verify(r => r.Update(hackathon), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(null), Times.Once);
        }
    }
}
