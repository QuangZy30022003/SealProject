using AutoMapper;
using Common.DTOs.CriterionDTO;
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
    public class CriterionServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private CriterionService _service;

        private Mock<IRepository<Criterion>> _criterionRepo;
        private Mock<IRepository<HackathonPhase>> _hackathonPhaseRepo;
        private Mock<IRepository<Track>> _trackRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _criterionRepo = new Mock<IRepository<Criterion>>();
            _hackathonPhaseRepo = new Mock<IRepository<HackathonPhase>>();
            _trackRepo = new Mock<IRepository<Track>>();

            _uowMock.Setup(u => u.Criteria).Returns(_criterionRepo.Object);
            _uowMock.Setup(u => u.HackathonPhases).Returns(_hackathonPhaseRepo.Object);
            _uowMock.Setup(u => u.Tracks).Returns(_trackRepo.Object);

            _service = new CriterionService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // 1. CreateAsync - Phase not found
        // =============================
        [Test]
        public void CreateAsync_WhenPhaseNotFound_Throws()
        {
            var dto = new CriterionCreateDto
            {
                PhaseId = 99,
                Criteria = new List<CriterionItemDto>
                {
                    new CriterionItemDto { Name = "Quality", Weight = 30 }
                }
            };

            _hackathonPhaseRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<HackathonPhase, bool>>>()))
                .ReturnsAsync(false);

            Func<Task> act = async () => await _service.CreateAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Phase not found");
        }

        // =============================
        // 2. CreateAsync - Track not found for phase
        // =============================
        [Test]
        public void CreateAsync_WhenTrackNotFoundForPhase_Throws()
        {
            var dto = new CriterionCreateDto
            {
                PhaseId = 1,
                Criteria = new List<CriterionItemDto>
                {
                    new CriterionItemDto { Name = "Quality", Weight = 30 }
                }
            };

            _hackathonPhaseRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<HackathonPhase, bool>>>()))
                .ReturnsAsync(true);
            _trackRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Track, bool>>>()))
                .ReturnsAsync(false);

            Func<Task> act = async () => await _service.CreateAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Track not found for this phase");
        }

        // =============================
        // 3. CreateAsync - No criteria provided
        // =============================
        [Test]
        public void CreateAsync_WhenNoCriteriaProvided_Throws()
        {
            var dto = new CriterionCreateDto
            {
                PhaseId = 1,
                Criteria = new List<CriterionItemDto>()
            };

            _hackathonPhaseRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<HackathonPhase, bool>>>()))
                .ReturnsAsync(true);

            Func<Task> act = async () => await _service.CreateAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("At least one criterion is required");
        }

        // =============================
        // 4. CreateAsync - Empty criterion name
        // =============================
        [Test]
        public void CreateAsync_WhenCriterionNameEmpty_Throws()
        {
            var dto = new CriterionCreateDto
            {
                PhaseId = 1,
                Criteria = new List<CriterionItemDto>
                {
                    new CriterionItemDto { Name = "", Weight = 30 }
                }
            };

            _hackathonPhaseRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<HackathonPhase, bool>>>()))
                .ReturnsAsync(true);

            Func<Task> act = async () => await _service.CreateAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Criterion name cannot be empty");
        }

        // =============================
        // 5. CreateAsync - Invalid weight
        // =============================
        [Test]
        public void CreateAsync_WhenWeightInvalid_Throws()
        {
            var dto = new CriterionCreateDto
            {
                PhaseId = 1,
                Criteria = new List<CriterionItemDto>
                {
                    new CriterionItemDto { Name = "Quality", Weight = 0 }
                }
            };

            _hackathonPhaseRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<HackathonPhase, bool>>>()))
                .ReturnsAsync(true);

            Func<Task> act = async () => await _service.CreateAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Weight must be greater than 0");
        }

        // =============================
        // 6. CreateAsync - Successful creation without track
        // =============================
        [Test]
        public async Task CreateAsync_WithoutTrack_ShouldCreateSuccessfully()
        {
            var dto = new CriterionCreateDto
            {
                PhaseId = 1,
                Criteria = new List<CriterionItemDto>
                {
                    new CriterionItemDto { Name = "Quality", Weight = 30 },
                    new CriterionItemDto { Name = "Innovation", Weight = 40 }
                }
            };
            var createdCriteria = new List<Criterion>
            {
                new Criterion { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 },
                new Criterion { CriteriaId = 2, PhaseId = 1, Name = "Innovation", Weight = 40 }
            };
            var responseDtos = new List<CriterionResponseDto>
            {
                new CriterionResponseDto { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 },
                new CriterionResponseDto { CriteriaId = 2, PhaseId = 1, Name = "Innovation", Weight = 40 }
            };

            _hackathonPhaseRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<HackathonPhase, bool>>>()))
                .ReturnsAsync(true);
            _criterionRepo.Setup(r => r.AddAsync(It.IsAny<Criterion>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<List<CriterionResponseDto>>(It.IsAny<List<Criterion>>())).Returns(responseDtos);

            var result = await _service.CreateAsync(dto);

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Quality");
            result.Last().Name.Should().Be("Innovation");

            _criterionRepo.Verify(r => r.AddAsync(It.IsAny<Criterion>()), Times.Exactly(2));
        }

        // =============================
        // 7. CreateAsync - Successful creation with track
        // =============================
        [Test]
        public async Task CreateAsync_WithTrack_ShouldCreateSuccessfully()
        {
            var dto = new CriterionCreateDto
            {
                PhaseId = 1,
                Criteria = new List<CriterionItemDto>
                {
                    new CriterionItemDto { Name = "Technical Skills", Weight = 50 }
                }
            };
            var createdCriteria = new List<Criterion>
            {
                new Criterion { CriteriaId = 1, PhaseId = 1, Name = "Technical Skills", Weight = 50 }
            };
            var responseDtos = new List<CriterionResponseDto>
            {
                new CriterionResponseDto { CriteriaId = 1, PhaseId = 1, Name = "Technical Skills", Weight = 50 }
            };

            _hackathonPhaseRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<HackathonPhase, bool>>>()))
                .ReturnsAsync(true);
            _trackRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Track, bool>>>()))
                .ReturnsAsync(true);
            _criterionRepo.Setup(r => r.AddAsync(It.IsAny<Criterion>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<List<CriterionResponseDto>>(It.IsAny<List<Criterion>>())).Returns(responseDtos);

            var result = await _service.CreateAsync(dto);

            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Technical Skills");

            _criterionRepo.Verify(r => r.AddAsync(It.IsAny<Criterion>()), Times.Once);
        }

        // =============================
        // 8. GetAllAsync - Return all criteria
        // =============================
        [Test]
        public async Task GetAllAsync_ShouldReturnAllCriteria()
        {
            var criteria = new List<Criterion>
            {
                new Criterion { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 },
                new Criterion { CriteriaId = 2, PhaseId = 1, Name = "Innovation", Weight = 40 }
            };
            var responseDtos = new List<CriterionResponseDto>
            {
                new CriterionResponseDto { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 },
                new CriterionResponseDto { CriteriaId = 2, PhaseId = 1, Name = "Innovation", Weight = 40 }
            };

            _criterionRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Criterion, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Criterion, object>>[]>()))
                .ReturnsAsync(criteria);
            _mapperMock.Setup(m => m.Map<List<CriterionResponseDto>>(criteria)).Returns(responseDtos);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Quality");
            result.Last().Name.Should().Be("Innovation");
        }

        // =============================
        // 9. GetAllAsync - Filter by phase
        // =============================
        [Test]
        public async Task GetAllAsync_WithPhaseFilter_ShouldReturnFilteredCriteria()
        {
            var criteria = new List<Criterion>
            {
                new Criterion { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 }
            };
            var responseDtos = new List<CriterionResponseDto>
            {
                new CriterionResponseDto { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 }
            };

            _criterionRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Criterion, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Criterion, object>>[]>()))
                .ReturnsAsync(criteria);
            _mapperMock.Setup(m => m.Map<List<CriterionResponseDto>>(criteria)).Returns(responseDtos);

            var result = await _service.GetAllAsync(1);

            result.Should().HaveCount(1);
            result.First().PhaseId.Should().Be(1);
        }

        // =============================
        // 10. GetByIdAsync - Criterion exists
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenCriterionExists_ShouldReturnCriterion()
        {
            var criterion = new Criterion { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 };
            var responseDto = new CriterionResponseDto { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 };

            _criterionRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Criterion, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Criterion, object>>[]>()))
                .ReturnsAsync(criterion);
            _mapperMock.Setup(m => m.Map<CriterionResponseDto>(criterion)).Returns(responseDto);

            var result = await _service.GetByIdAsync(1);

            result.Should().NotBeNull();
            result.Name.Should().Be("Quality");
            result.Weight.Should().Be(30);
        }

        // =============================
        // 11. GetByIdAsync - Criterion not exists
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenCriterionNotExists_ShouldReturnNull()
        {
            _criterionRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Criterion, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Criterion, object>>[]>()))
                .ReturnsAsync((Criterion)null);

            var result = await _service.GetByIdAsync(99);

            result.Should().BeNull();
        }

        // =============================
        // 12. UpdateAsync - Criterion not found
        // =============================
        [Test]
        public async Task UpdateAsync_WhenCriterionNotFound_ReturnsNull()
        {
            var dto = new CriterionUpdateDto
            {
                Name = "Updated Quality",
                Weight = 35,
            };

            _criterionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Criterion)null);

            var result = await _service.UpdateAsync(99, dto);

            result.Should().BeNull();
        }

        // =============================
        // 13. UpdateAsync - Track not found for phase
        // =============================
        [Test]
        public void UpdateAsync_WhenTrackNotFoundForPhase_Throws()
        {
            var dto = new CriterionUpdateDto
            {
                Name = "Updated Quality",
                Weight = 35,
            };
            var criterion = new Criterion { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 };

            _criterionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(criterion);
            _trackRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Track, bool>>>()))
                .ReturnsAsync(false);

            Func<Task> act = async () => await _service.UpdateAsync(1, dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Track not found for this phase");
        }

        // =============================
        // 14. UpdateAsync - Successful update
        // =============================
        [Test]
        public async Task UpdateAsync_WhenValidData_ShouldUpdateSuccessfully()
        {
            var dto = new CriterionUpdateDto
            {
                Name = "Updated Quality",
                Weight = 35,
            };
            var criterion = new Criterion { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 };
            var responseDto = new CriterionResponseDto { CriteriaId = 1, PhaseId = 1, Name = "Updated Quality", Weight = 35 };

            _criterionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(criterion);
            _trackRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Track, bool>>>()))
                .ReturnsAsync(true);
            _criterionRepo.Setup(r => r.Update(It.IsAny<Criterion>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<CriterionResponseDto>(criterion)).Returns(responseDto);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().NotBeNull();
            result.Name.Should().Be("Updated Quality");
            result.Weight.Should().Be(35);

            criterion.Name.Should().Be("Updated Quality");
            criterion.Weight.Should().Be(35);

            _criterionRepo.Verify(r => r.Update(criterion), Times.Once);
        }

        // =============================
        // 15. UpdateAsync - Update without track
        // =============================
        [Test]
        public async Task UpdateAsync_WithoutTrack_ShouldUpdateSuccessfully()
        {
            var dto = new CriterionUpdateDto
            {
                Name = "Updated Quality",
                Weight = 35,
            };
            var criterion = new Criterion { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 };
            var responseDto = new CriterionResponseDto { CriteriaId = 1, PhaseId = 1, Name = "Updated Quality", Weight = 35 };

            _criterionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(criterion);
            _criterionRepo.Setup(r => r.Update(It.IsAny<Criterion>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<CriterionResponseDto>(criterion)).Returns(responseDto);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().NotBeNull();
            result.Name.Should().Be("Updated Quality");

        }

        // =============================
        // 16. DeleteAsync - Criterion not found
        // =============================
        [Test]
        public async Task DeleteAsync_WhenCriterionNotFound_ReturnsFalse()
        {
            _criterionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Criterion)null);

            var result = await _service.DeleteAsync(99);

            result.Should().BeFalse();
        }

        // =============================
        // 17. DeleteAsync - Successful deletion
        // =============================
        [Test]
        public async Task DeleteAsync_WhenCriterionExists_ShouldDeleteSuccessfully()
        {
            var criterion = new Criterion { CriteriaId = 1, PhaseId = 1, Name = "Quality", Weight = 30 };

            _criterionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(criterion);
            _criterionRepo.Setup(r => r.Remove(It.IsAny<Criterion>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.DeleteAsync(1);

            result.Should().BeTrue();
            _criterionRepo.Verify(r => r.Remove(criterion), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }
    }
}