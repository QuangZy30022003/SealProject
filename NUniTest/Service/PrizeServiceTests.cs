using AutoMapper;
using Common.DTOs.PrizeDto;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Servicefolder;

namespace NUniTest.Service
{
    [TestFixture]
    public class PrizeServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private PrizeService _service;

        private Mock<IRepository<Prize>> _prizeRepo;
        private Mock<IPrizeRepository> _prizeRepos;
        private Mock<IRepository<Hackathon>> _hackathonRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _prizeRepo = new Mock<IRepository<Prize>>();
            _hackathonRepo = new Mock<IRepository<Hackathon>>();
            _prizeRepos = new Mock<IPrizeRepository>();

            _uowMock.Setup(u => u.PrizeRepository).Returns(_prizeRepos.Object);
            _uowMock.Setup(u => u.Prizes).Returns(_prizeRepo.Object);
            _uowMock.Setup(u => u.Hackathons).Returns(_hackathonRepo.Object);

            _service = new PrizeService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // 1. GetAllAsync - Return all prizes
        // =============================
        [Test]
        public async Task GetAllAsync_ShouldReturnAllPrizes()
        {
            var prizes = new List<Prize>
            {
                new Prize 
                { 
                    PrizeId = 1, 
                    PrizeName = "First Place", 
                    HackathonId = 1,
                    Hackathon = new Hackathon { HackathonId = 1, Name = "Test Hackathon" }
                },
                new Prize 
                { 
                    PrizeId = 2, 
                    PrizeName = "Second Place", 
                    HackathonId = 1,
                    Hackathon = new Hackathon { HackathonId = 1, Name = "Test Hackathon" }
                }
            };
            var prizeDtos = new List<PrizeDTO>
            {
                new PrizeDTO { PrizeId = 1, PrizeName = "First Place", HackathonId = 1 },
                new PrizeDTO { PrizeId = 2, PrizeName = "Second Place", HackathonId = 1 }
            };

            // ✅ Fix: Use PrizeRepository instead of Prizes
            _prizeRepos.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Prize, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Prize, object>>[]>()))
                .ReturnsAsync(prizes);
            _mapperMock.Setup(m => m.Map<IEnumerable<PrizeDTO>>(prizes)).Returns(prizeDtos);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().PrizeName.Should().Be("First Place");
            result.Last().PrizeName.Should().Be("Second Place");
        }

        // =============================
        // 2. GetByHackathonAsync - Return prizes by hackathon
        // =============================
        [Test]
        public async Task GetByHackathonAsync_ShouldReturnPrizesByHackathon()
        {
            var prizes = new List<Prize>
            {
                new Prize { PrizeId = 1, PrizeName = "First Place", HackathonId = 1 },
                new Prize { PrizeId = 2, PrizeName = "Second Place", HackathonId = 1 }
            };
            var prizeDtos = new List<PrizeDTO>
            {
                new PrizeDTO { PrizeId = 1, PrizeName = "First Place", HackathonId = 1 },
                new PrizeDTO { PrizeId = 2, PrizeName = "Second Place", HackathonId = 1 }
            };

            _prizeRepos.Setup(r => r.GetPrizesByHackathonIdAsync(1)).ReturnsAsync(prizes);
            _mapperMock.Setup(m => m.Map<IEnumerable<PrizeDTO>>(prizes)).Returns(prizeDtos);

            var result = await _service.GetByHackathonAsync(1);

            result.Should().HaveCount(2);
            result.All(p => p.HackathonId == 1).Should().BeTrue();
        }

        // =============================
        // 3. CreateAsync - Hackathon not found
        // =============================
        [Test]
        public void CreateAsync_WhenHackathonNotFound_Throws()
        {
            var dto = new CreatePrizeDTO
            {
                PrizeName = "New Prize",
                HackathonId = 99,
                Rank = 1000
            };

            _hackathonRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Hackathon, bool>>>()))
                .ReturnsAsync(false);

            Func<Task> act = async () => await _service.CreateAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Hackathon không tồn tại.");
        }

        // =============================
        // 4. CreateAsync - Successful creation
        // =============================
        [Test]
        public async Task CreateAsync_WhenValidData_ShouldCreateSuccessfully()
        {
            var dto = new CreatePrizeDTO
            {
                PrizeName = "New Prize",
                HackathonId = 1,
                Rank = 1000,
                Reward = "Test prize description"
            };
            var prize = new Prize 
            { 
                PrizeId = 1, 
                PrizeName = "New Prize", 
                HackathonId = 1,
                Rank = 1000,
                Reward = "Test prize description"
            };
            var prizeDto = new PrizeDTO 
            { 
                PrizeId = 1, 
                PrizeName = "New Prize", 
                HackathonId = 1,
                Rank = 1000,
                Reward = "Test prize description"
            };

            _hackathonRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Hackathon, bool>>>()))
                .ReturnsAsync(true);
            _mapperMock.Setup(m => m.Map<Prize>(dto)).Returns(prize);
            // ✅ Fix: Use PrizeRepository instead of Prizes
            _prizeRepos.Setup(r => r.AddAsync(It.IsAny<Prize>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<PrizeDTO>(prize)).Returns(prizeDto);

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.PrizeName.Should().Be("New Prize");
            result.Rank.Should().Be(1000);

            // ✅ Fix: Verify on PrizeRepository
            _prizeRepos.Verify(r => r.AddAsync(prize), Times.Once);
        }

        // =============================
        // 5. UpdateAsync - Prize not found
        // =============================
        [Test]
        public void UpdateAsync_WhenPrizeNotFound_Throws()
        {
            var dto = new UpdatePrizeDTO
            {
                PrizeId = 99,
                PrizeName = "Updated Prize",
                Rank = 1500
            };

            // ✅ Fix: Use PrizeRepository instead of Prizes
            _prizeRepos.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Prize)null);

            Func<Task> act = async () => await _service.UpdateAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Giải thưởng không tồn tại.");
        }

        // =============================
        // 6. UpdateAsync - Successful update
        // =============================
        [Test]
        public async Task UpdateAsync_WhenValidData_ShouldUpdateSuccessfully()
        {
            var dto = new UpdatePrizeDTO
            {
                PrizeId = 1,
                PrizeName = "Updated Prize",
                Rank = 1500,
                Reward = "Updated description"
            };
            var prize = new Prize 
            { 
                PrizeId = 1, 
                PrizeName = "Original Prize", 
                HackathonId = 1,
                Rank = 1000,
                Reward = "Original description"
            };
            var updatedPrizeDto = new PrizeDTO 
            { 
                PrizeId = 1, 
                PrizeName = "Updated Prize", 
                HackathonId = 1,
                Rank = 1500,
                Reward = "Updated description"
            };

            // ✅ Fix: Use PrizeRepository instead of Prizes
            _prizeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prize);
            _mapperMock.Setup(m => m.Map(dto, prize));
            _prizeRepos.Setup(r => r.Update(It.IsAny<Prize>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<PrizeDTO>(prize)).Returns(updatedPrizeDto);

            var result = await _service.UpdateAsync(dto);

            result.Should().NotBeNull();
            result.PrizeName.Should().Be("Updated Prize");
            result.Rank.Should().Be(1500);

            _mapperMock.Verify(m => m.Map(dto, prize), Times.Once);
            // ✅ Fix: Verify on PrizeRepository
            _prizeRepos.Verify(r => r.Update(prize), Times.Once);
        }

        // =============================
        // 7. DeleteAsync - Prize not found
        // =============================
        [Test]
        public void DeleteAsync_WhenPrizeNotFound_Throws()
        {
            // ✅ Fix: Use PrizeRepository instead of Prizes
            _prizeRepos.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Prize)null);

            Func<Task> act = async () => await _service.DeleteAsync(99);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Giải thưởng không tồn tại.");
        }

        // =============================
        // 8. DeleteAsync - Successful deletion
        // =============================
        [Test]
        public async Task DeleteAsync_WhenPrizeExists_ShouldDeleteSuccessfully()
        {
            var prize = new Prize 
            { 
                PrizeId = 1, 
                PrizeName = "Prize to Delete", 
                HackathonId = 1, 
                Rank = 1000
            };

            // ✅ Fix: Use PrizeRepository instead of Prizes
            _prizeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prize);
            _prizeRepos.Setup(r => r.Remove(It.IsAny<Prize>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            await _service.DeleteAsync(1);

            // ✅ Fix: Verify on PrizeRepository
            _prizeRepos.Verify(r => r.Remove(prize), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }

        // =============================
        // 9. GetAllAsync - Empty result
        // =============================
        [Test]
        public async Task GetAllAsync_WhenNoPrizes_ShouldReturnEmptyList()
        {
            var emptyPrizes = new List<Prize>();
            var emptyPrizeDtos = new List<PrizeDTO>();

            // ✅ Fix: Use PrizeRepository instead of Prizes
            _prizeRepos.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Prize, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Prize, object>>[]>()))
                .ReturnsAsync(emptyPrizes);
            _mapperMock.Setup(m => m.Map<IEnumerable<PrizeDTO>>(emptyPrizes)).Returns(emptyPrizeDtos);

            var result = await _service.GetAllAsync();

            result.Should().BeEmpty();
        }

        // =============================
        // 10. GetByHackathonAsync - Empty result for hackathon
        // =============================
        [Test]
        public async Task GetByHackathonAsync_WhenNoPrizesForHackathon_ShouldReturnEmptyList()
        {
            var emptyPrizes = new List<Prize>();
            var emptyPrizeDtos = new List<PrizeDTO>();

            _prizeRepos.Setup(r => r.GetPrizesByHackathonIdAsync(99)).ReturnsAsync(emptyPrizes);
            _mapperMock.Setup(m => m.Map<IEnumerable<PrizeDTO>>(emptyPrizes)).Returns(emptyPrizeDtos);

            var result = await _service.GetByHackathonAsync(99);

            result.Should().BeEmpty();
        }

        // =============================
        // 11. CreateAsync - Verify all repository calls
        // =============================
        [Test]
        public async Task CreateAsync_ShouldCallAllRepositoryMethods()
        {
            var dto = new CreatePrizeDTO
            {
                PrizeName = "Test Prize",
                HackathonId = 1,
                Rank = 500
            };
            var prize = new Prize { PrizeId = 1, PrizeName = "Test Prize", HackathonId = 1, Rank = 500 };
            var prizeDto = new PrizeDTO { PrizeId = 1, PrizeName = "Test Prize", HackathonId = 1, Rank = 500 };

            _hackathonRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Hackathon, bool>>>()))
                .ReturnsAsync(true);
            _mapperMock.Setup(m => m.Map<Prize>(dto)).Returns(prize);
            // ✅ Fix: Use PrizeRepository instead of Prizes
            _prizeRepos.Setup(r => r.AddAsync(It.IsAny<Prize>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<PrizeDTO>(prize)).Returns(prizeDto);

            await _service.CreateAsync(dto);

            _hackathonRepo.Verify(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Hackathon, bool>>>()), Times.Once);
            _mapperMock.Verify(m => m.Map<Prize>(dto), Times.Once);
            // ✅ Fix: Verify on PrizeRepository
            _prizeRepos.Verify(r => r.AddAsync(prize), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
            _mapperMock.Verify(m => m.Map<PrizeDTO>(prize), Times.Once);
        }

        // =============================
        // 12. UpdateAsync - Verify all repository calls
        // =============================
        [Test]
        public async Task UpdateAsync_ShouldCallAllRepositoryMethods()
        {
            var dto = new UpdatePrizeDTO
            {
                PrizeId = 1,
                PrizeName = "Updated Prize",
                Rank = 2000
            };
            var prize = new Prize { PrizeId = 1, PrizeName = "Original Prize", Rank = 1000 };
            var updatedPrizeDto = new PrizeDTO { PrizeId = 1, PrizeName = "Updated Prize", Rank = 2000 };

            // ✅ Fix: Use PrizeRepository instead of Prizes
            _prizeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(prize);
            _mapperMock.Setup(m => m.Map(dto, prize));
            _prizeRepos.Setup(r => r.Update(It.IsAny<Prize>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<PrizeDTO>(prize)).Returns(updatedPrizeDto);

            await _service.UpdateAsync(dto);

            // ✅ Fix: Verify on PrizeRepository
            _prizeRepos.Verify(r => r.GetByIdAsync(1), Times.Once);
            _mapperMock.Verify(m => m.Map(dto, prize), Times.Once);
            _prizeRepos.Verify(r => r.Update(prize), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
            _mapperMock.Verify(m => m.Map<PrizeDTO>(prize), Times.Once);
        }
    }
}