using AutoMapper;
using Common.DTOs.SeasonDto;
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
    public class SeasonServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private SeasonService _service;

        private Mock<IRepository<Season>> _seasonRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _seasonRepo = new Mock<IRepository<Season>>();

            _uowMock.Setup(u => u.Seasons).Returns(_seasonRepo.Object);

            _service = new SeasonService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // 1. GetAllSeasonsAsync - Return all seasons
        // =============================
        [Test]
        public async Task GetAllSeasonsAsync_ShouldReturnAllSeasons()
        {
            var seasons = new List<Season>
            {
                new Season { SeasonId = 1, Name = "Season 1", Code = "S1" },
                new Season { SeasonId = 2, Name = "Season 2", Code = "S2" }
            };
            var seasonResponses = new List<SeasonResponse>
            {
                new SeasonResponse { SeasonId = 1, Name = "Season 1", SeasonCode = "S1" },
                new SeasonResponse { SeasonId = 2, Name = "Season 2", SeasonCode = "S2" }
            };

            _seasonRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<Season, bool>>>(),
        It.IsAny<Func<IQueryable<Season>, IOrderedQueryable<Season>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(seasons);

            _mapperMock.Setup(m => m.Map<IEnumerable<SeasonResponse>>(seasons)).Returns(seasonResponses);

            var result = await _service.GetAllSeasonsAsync();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Season 1");
            result.Last().Name.Should().Be("Season 2");
        }

        // =============================
        // 2. GetByIdAsync - Season exists
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenSeasonExists_ShouldReturnSeason()
        {
            var season = new Season { SeasonId = 1, Name = "Test Season", Code = "TS" };
            var seasonResponse = new SeasonResponse { SeasonId = 1, Name = "Test Season", SeasonCode = "TS" };

            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);
            _mapperMock.Setup(m => m.Map<SeasonResponse>(season)).Returns(seasonResponse);

            var result = await _service.GetByIdAsync(1);

            result.Should().NotBeNull();
            result.Name.Should().Be("Test Season");
            result.SeasonCode.Should().Be("TS");
        }

        // =============================
        // 3. GetByIdAsync - Season not exists
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenSeasonNotExists_ShouldReturnNull()
        {
            _seasonRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Season)null);
            _mapperMock.Setup(m => m.Map<SeasonResponse>(It.IsAny<Season>())).Returns((SeasonResponse)null);

            var result = await _service.GetByIdAsync(99);

            result.Should().BeNull();
        }

        // =============================
        // 4. CreateAsync - Duplicate season code
        // =============================
        [Test]
        public async Task CreateAsync_WhenSeasonCodeExists_ReturnsErrorMessage()
        {
            var dto = new SeasonRequest
            {
                SeasonCode = "EXISTING",
                Name = "New Season"
            };
            var existingSeason = new Season { SeasonId = 1, Code = "existing", Name = "Old Season" };

            _seasonRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Season, bool>>>()))
                .ReturnsAsync(existingSeason);

            var result = await _service.CreateAsync(dto);

            result.Should().Be("Season Code 'EXISTING' already exists.");
        }

        // =============================
        // 5. CreateAsync - Duplicate season name
        // =============================
        [Test]
        public async Task CreateAsync_WhenSeasonNameExists_ReturnsErrorMessage()
        {
            var dto = new SeasonRequest
            {
                SeasonCode = "NEW",
                Name = "Existing Season"
            };
            var existingSeason = new Season { SeasonId = 1, Code = "OLD", Name = "existing season" };

            _seasonRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Season, bool>>>()))
                .ReturnsAsync((Season)null) // First call for code check
                .ReturnsAsync(existingSeason); // Second call for name check

            var result = await _service.CreateAsync(dto);

            result.Should().Be("Season Name 'Existing Season' already exists.");
        }

        // =============================
        // 6. CreateAsync - Successful creation
        // =============================
        [Test]
        public async Task CreateAsync_WhenValidData_ShouldCreateSuccessfully()
        {
            var dto = new SeasonRequest
            {
                SeasonCode = "NEW",
                Name = "New Season"
            };
            var season = new Season { SeasonId = 1, Code = "NEW", Name = "New Season" };

            _seasonRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Season, bool>>>()))
                .ReturnsAsync((Season)null);
            _mapperMock.Setup(m => m.Map<Season>(dto)).Returns(season);
            _seasonRepo.Setup(r => r.AddAsync(It.IsAny<Season>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.CreateAsync(dto);

            result.Should().Be("Season created successfully!");
            _seasonRepo.Verify(r => r.AddAsync(season), Times.Once);
        }

        // =============================
        // 7. UpdateAsync - Season not found
        // =============================
        [Test]
        public async Task UpdateAsync_WhenSeasonNotFound_ReturnsErrorMessage()
        {
            var dto = new SeasonUpdateDto
            {
                SeasonCode = "UPD",
                Name = "Updated Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _seasonRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Season)null);

            var result = await _service.UpdateAsync(99, dto);

            result.Should().Be("Season with ID 99 not found.");
        }

        // =============================
        // 8. UpdateAsync - StartDate >= EndDate
        // =============================
        [Test]
        public async Task UpdateAsync_WhenStartDateAfterEndDate_ReturnsErrorMessage()
        {
            var dto = new SeasonUpdateDto
            {
                SeasonCode = "UPD",
                Name = "Updated Season",
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow // EndDate before StartDate
            };
            var season = new Season { SeasonId = 1, Code = "OLD", Name = "Old Season" };

            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().Be("StartDate must be earlier than EndDate.");
        }

        // =============================
        // 9. UpdateAsync - Duplicate code (different season)
        // =============================
        [Test]
        public async Task UpdateAsync_WhenCodeExistsInOtherSeason_ReturnsErrorMessage()
        {
            var dto = new SeasonUpdateDto
            {
                SeasonCode = "EXISTING",
                Name = "Updated Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };
            var season = new Season { SeasonId = 1, Code = "OLD", Name = "Old Season" };
            var existingSeason = new Season { SeasonId = 2, Code = "EXISTING", Name = "Other Season" };

            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);
            _seasonRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Season, bool>>>()))
                .ReturnsAsync(existingSeason) // Code check
                .ReturnsAsync((Season)null); // Name check

            var result = await _service.UpdateAsync(1, dto);

            result.Should().Be("Season Code 'EXISTING' already exists.");
        }

        // =============================
        // 10. UpdateAsync - Duplicate name (different season)
        // =============================
        [Test]
        public async Task UpdateAsync_WhenNameExistsInOtherSeason_ReturnsErrorMessage()
        {
            var dto = new SeasonUpdateDto
            {
                SeasonCode = "UPD",
                Name = "Existing Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };
            var season = new Season { SeasonId = 1, Code = "OLD", Name = "Old Season" };
            var existingSeason = new Season { SeasonId = 2, Code = "OTHER", Name = "Existing Season" };

            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);
            _seasonRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Season, bool>>>()))
                .ReturnsAsync((Season)null) // Code check
                .ReturnsAsync(existingSeason); // Name check

            var result = await _service.UpdateAsync(1, dto);

            result.Should().Be("Season Name 'Existing Season' already exists.");
        }

        // =============================
        // 11. UpdateAsync - Successful update
        // =============================
        [Test]
        public async Task UpdateAsync_WhenValidData_ShouldUpdateSuccessfully()
        {
            var dto = new SeasonUpdateDto
            {
                SeasonCode = "UPD",
                Name = "Updated Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };
            var season = new Season { SeasonId = 1, Code = "OLD", Name = "Old Season" };

            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);
            _seasonRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Season, bool>>>()))
                .ReturnsAsync((Season)null);
            _mapperMock.Setup(m => m.Map(dto, season));
            _seasonRepo.Setup(r => r.Update(It.IsAny<Season>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().Be("Season updated successfully!");
            _seasonRepo.Verify(r => r.Update(season), Times.Once);
            _mapperMock.Verify(m => m.Map(dto, season), Times.Once);
        }

        // =============================
        // 12. UpdateAsync - Update same season (code and name should be allowed)
        // =============================
        [Test]
        public async Task UpdateAsync_WhenUpdatingSameSeason_ShouldAllowSameCodeAndName()
        {
            var dto = new SeasonUpdateDto
            {
                SeasonCode = "SAME",
                Name = "Same Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };
            var season = new Season { SeasonId = 1, Code = "SAME", Name = "Same Season" };

            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);
            _seasonRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Season, bool>>>()))
                .ReturnsAsync(season); // Returns same season for both checks
            _mapperMock.Setup(m => m.Map(dto, season));
            _seasonRepo.Setup(r => r.Update(It.IsAny<Season>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().Be("Season updated successfully!");
            _seasonRepo.Verify(r => r.Update(season), Times.Once);
        }

        // =============================
        // 13. DeleteAsync - Season not found
        // =============================
        [Test]
        public async Task DeleteAsync_WhenSeasonNotFound_ReturnsFalse()
        {
            _seasonRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Season)null);

            var result = await _service.DeleteAsync(99);

            result.Should().BeFalse();
        }

        // =============================
        // 14. DeleteAsync - Successful deletion
        // =============================
        [Test]
        public async Task DeleteAsync_WhenSeasonExists_ShouldDeleteSuccessfully()
        {
            var season = new Season { SeasonId = 1, Code = "DEL", Name = "Season to Delete" };

            _seasonRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(season);
            _seasonRepo.Setup(r => r.Remove(It.IsAny<Season>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.DeleteAsync(1);

            result.Should().BeTrue();
            _seasonRepo.Verify(r => r.Remove(season), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(null), Times.Once);
        }
    }
}