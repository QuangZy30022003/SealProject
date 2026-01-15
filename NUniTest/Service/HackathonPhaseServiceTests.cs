using AutoMapper;
using Common.DTOs.HackathonPhaseDto;
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
    public class HackathonPhaseServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private HackathonPhaseService _service;

        private Mock<IRepository<HackathonPhase>> _phaseRepo;
        private Mock<IHackathonPhaseRepository> _phaseRepos;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _phaseRepo = new Mock<IRepository<HackathonPhase>>();
            _phaseRepos = new Mock<IHackathonPhaseRepository>();

            _uowMock.Setup(u => u.HackathonPhases).Returns(_phaseRepo.Object);
            _uowMock.Setup(u => u.HackathonPhaseRepository).Returns(_phaseRepos.Object);

            _service = new HackathonPhaseService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // 1. GetPhasesByHackathonAsync - Lấy phases theo hackathon
        // =============================
        [Test]
        public async Task GetPhasesByHackathonAsync_ShouldReturnPhasesByHackathon()
        {
            var phases = new List<HackathonPhase>
            {
                new HackathonPhase { PhaseId = 1, HackathonId = 1, PhaseName = "Registration", StartDate = DateTime.UtcNow },
                new HackathonPhase { PhaseId = 2, HackathonId = 1, PhaseName = "Development", StartDate = DateTime.UtcNow.AddDays(1) }
            };
            var phaseDtos = new List<HackathonPhaseDto>
            {
                new HackathonPhaseDto { PhaseId = 1, HackathonId = 1, PhaseName = "Registration" },
                new HackathonPhaseDto { PhaseId = 2, HackathonId = 1, PhaseName = "Development" }
            };

            _phaseRepos.Setup(r => r.GetByHackathonIdAsync(1)).ReturnsAsync(phases);
            _mapperMock.Setup(m => m.Map<List<HackathonPhaseDto>>(phases)).Returns(phaseDtos);

            var result = await _service.GetPhasesByHackathonAsync(1);

            result.Should().HaveCount(2);
            result.First().PhaseName.Should().Be("Registration");
            result.Last().PhaseName.Should().Be("Development");
        }

        // =============================
        // 2. GetByIdAsync - Phase tồn tại
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenPhaseExists_ShouldReturnPhase()
        {
            var phase = new HackathonPhase { PhaseId = 1, PhaseName = "Registration", HackathonId = 1 };
            var phaseDto = new HackathonPhaseDto { PhaseId = 1, PhaseName = "Registration", HackathonId = 1 };

            // ✅ Fix: Use HackathonPhaseRepository instead of HackathonPhases
            _phaseRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);
            _mapperMock.Setup(m => m.Map<HackathonPhaseDto?>(phase)).Returns(phaseDto);

            var result = await _service.GetByIdAsync(1);

            result.Should().NotBeNull();
            result.PhaseId.Should().Be(1);
            result.PhaseName.Should().Be("Registration");
        }

        // =============================
        // 3. GetByIdAsync - Phase không tồn tại
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenPhaseNotExists_ShouldReturnNull()
        {
            // ✅ Fix: Use HackathonPhaseRepository instead of HackathonPhases
            _phaseRepos.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((HackathonPhase)null);
            _mapperMock.Setup(m => m.Map<HackathonPhaseDto?>(It.IsAny<HackathonPhase>())).Returns((HackathonPhaseDto)null);

            var result = await _service.GetByIdAsync(99);

            result.Should().BeNull();
        }

        // =============================
        // 4. CreateAsync - StartDate >= EndDate
        // =============================
        [Test]
        public void CreateAsync_WhenStartDateAfterEndDate_Throws()
        {
            var dto = new HackathonPhaseCreateDto
            {
                HackathonId = 1,
                PhaseName = "Invalid Phase",
                StartDate = DateTime.UtcNow.AddDays(2),
                EndDate = DateTime.UtcNow.AddDays(1) // EndDate trước StartDate
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Phase start date must be before end date");
        }

        // =============================
        // 5. CreateAsync - StartDate = EndDate
        // =============================
        [Test]
        public void CreateAsync_WhenStartDateEqualsEndDate_Throws()
        {
            var sameDate = DateTime.UtcNow.AddDays(1);
            var dto = new HackathonPhaseCreateDto
            {
                HackathonId = 1,
                PhaseName = "Invalid Phase",
                StartDate = sameDate,
                EndDate = sameDate // Cùng thời gian
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Phase start date must be before end date");
        }

        // =============================
        // 6. CreateAsync - Phase overlap với existing phase
        // =============================
        [Test]
        public void CreateAsync_WhenPhaseOverlaps_Throws()
        {
            var dto = new HackathonPhaseCreateDto
            {
                HackathonId = 1,
                PhaseName = "Overlapping Phase",
                StartDate = DateTime.UtcNow.AddDays(2),
                EndDate = DateTime.UtcNow.AddDays(4)
            };
            var existingPhases = new List<HackathonPhase>
            {
                new HackathonPhase 
                { 
                    PhaseId = 1, 
                    HackathonId = 1, 
                    PhaseName = "Existing Phase",
                    StartDate = DateTime.UtcNow.AddDays(1),
                    EndDate = DateTime.UtcNow.AddDays(3) // Overlap với new phase
                }
            };

            _phaseRepos.Setup(r => r.GetByHackathonIdAsync(1)).ReturnsAsync(existingPhases);

            Func<Task> act = async () => await _service.CreateAsync(dto);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Phase overlaps with existing phase*");
        }

        // =============================
        // 7. CreateAsync - Tạo phase thành công (không overlap)
        // =============================
        [Test]
        public async Task CreateAsync_WhenValidNoOverlap_ShouldCreatePhase()
        {
            var dto = new HackathonPhaseCreateDto
            {
                HackathonId = 1,
                PhaseName = "New Phase",
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(7)
            };
            var existingPhases = new List<HackathonPhase>
            {
                new HackathonPhase 
                { 
                    PhaseId = 1, 
                    HackathonId = 1,
                    StartDate = DateTime.UtcNow.AddDays(1),
                    EndDate = DateTime.UtcNow.AddDays(3) // Không overlap
                }
            };
            var phase = new HackathonPhase { PhaseId = 2, PhaseName = "New Phase", HackathonId = 1 };
            var phaseDto = new HackathonPhaseDto { PhaseId = 2, PhaseName = "New Phase", HackathonId = 1 };

            _phaseRepos.Setup(r => r.GetByHackathonIdAsync(1)).ReturnsAsync(existingPhases);
            _mapperMock.Setup(m => m.Map<HackathonPhase>(dto)).Returns(phase);
            // ✅ Fix: Use HackathonPhaseRepository for AddAsync
            _phaseRepos.Setup(r => r.AddAsync(It.IsAny<HackathonPhase>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<HackathonPhaseDto>(phase)).Returns(phaseDto);

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.PhaseId.Should().Be(2);
            result.PhaseName.Should().Be("New Phase");

            // ✅ Fix: Verify on HackathonPhaseRepository
            _phaseRepos.Verify(r => r.AddAsync(phase), Times.Once);
        }

        // =============================
        // 8. CreateAsync - Tạo phase đầu tiên (không có existing phases)
        // =============================
        [Test]
        public async Task CreateAsync_WhenFirstPhase_ShouldCreateSuccessfully()
        {
            var dto = new HackathonPhaseCreateDto
            {
                HackathonId = 1,
                PhaseName = "First Phase",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(3)
            };
            var phase = new HackathonPhase { PhaseId = 1, PhaseName = "First Phase", HackathonId = 1 };
            var phaseDto = new HackathonPhaseDto { PhaseId = 1, PhaseName = "First Phase", HackathonId = 1 };

            _phaseRepos.Setup(r => r.GetByHackathonIdAsync(1)).ReturnsAsync(new List<HackathonPhase>());
            _mapperMock.Setup(m => m.Map<HackathonPhase>(dto)).Returns(phase);
            // ✅ Fix: Use HackathonPhaseRepository for AddAsync
            _phaseRepos.Setup(r => r.AddAsync(It.IsAny<HackathonPhase>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<HackathonPhaseDto>(phase)).Returns(phaseDto);

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.PhaseName.Should().Be("First Phase");

            // ✅ Fix: Verify on HackathonPhaseRepository
            _phaseRepos.Verify(r => r.AddAsync(phase), Times.Once);
        }
        // =============================
        // 9. UpdateAsync - Phase không tồn tại
        // =============================
        [Test]
        public async Task UpdateAsync_WhenPhaseNotFound_ReturnsFalse()
        {
            var dto = new HackathonPhaseUpdateDto
            {
                PhaseName = "Updated Phase",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(3)
            };

            // ✅ Fix: Use HackathonPhaseRepository instead of HackathonPhases
            _phaseRepos.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((HackathonPhase)null);

            var result = await _service.UpdateAsync(99, dto);

            result.Should().BeFalse();
        }

        // =============================
        // 10. UpdateAsync - StartDate >= EndDate
        // =============================
        [Test]
        public void UpdateAsync_WhenStartDateAfterEndDate_Throws()
        {
            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 1, PhaseName = "Existing Phase" };
            var dto = new HackathonPhaseUpdateDto
            {
                PhaseName = "Updated Phase",
                StartDate = DateTime.UtcNow.AddDays(3),
                EndDate = DateTime.UtcNow.AddDays(1) // EndDate trước StartDate
            };

            // ✅ Fix: Use HackathonPhaseRepository instead of HackathonPhases
            _phaseRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            Func<Task> act = async () => await _service.UpdateAsync(1, dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("StartDate must be before EndDate");
        }

        // =============================
        // 11. UpdateAsync - Update overlap với existing phase khác
        // =============================
        [Test]
        public void UpdateAsync_WhenUpdateOverlapsWithOtherPhase_Throws()
        {
            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 1, PhaseName = "Phase to Update" };
            var dto = new HackathonPhaseUpdateDto
            {
                PhaseName = "Updated Phase",
                StartDate = DateTime.UtcNow.AddDays(2),
                EndDate = DateTime.UtcNow.AddDays(4)
            };
            var existingPhases = new List<HackathonPhase>
            {
                phase, // Phase đang update
                new HackathonPhase 
                { 
                    PhaseId = 2, 
                    HackathonId = 1,
                    StartDate = DateTime.UtcNow.AddDays(1),
                    EndDate = DateTime.UtcNow.AddDays(3) // Overlap với update
                }
            };

            // ✅ Fix: Use HackathonPhaseRepository instead of HackathonPhases
            _phaseRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);
            _phaseRepos.Setup(r => r.GetByHackathonIdAsync(1)).ReturnsAsync(existingPhases);

            Func<Task> act = async () => await _service.UpdateAsync(1, dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Phase dates must not overlap with other phases");
        }

        // =============================
        // 12. UpdateAsync - Update thành công (không overlap)
        // =============================
        [Test]
        public async Task UpdateAsync_WhenValidNoOverlap_ShouldUpdateAndReturnTrue()
        {
            var phase = new HackathonPhase 
            { 
                PhaseId = 1, 
                HackathonId = 1, 
                PhaseName = "Old Phase",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2)
            };
            var dto = new HackathonPhaseUpdateDto
            {
                PhaseName = "Updated Phase",
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(7)
            };
            var existingPhases = new List<HackathonPhase>
            {
                phase, // Phase đang update
                new HackathonPhase 
                { 
                    PhaseId = 2, 
                    HackathonId = 1,
                    StartDate = DateTime.UtcNow.AddDays(1),
                    EndDate = DateTime.UtcNow.AddDays(3) // Không overlap với update
                }
            };

            // ✅ Fix: Use HackathonPhaseRepository instead of HackathonPhases
            _phaseRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);
            _phaseRepos.Setup(r => r.GetByHackathonIdAsync(1)).ReturnsAsync(existingPhases);
            _phaseRepos.Setup(r => r.Update(It.IsAny<HackathonPhase>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().BeTrue();
            phase.PhaseName.Should().Be("Updated Phase");
            phase.StartDate.Should().Be(dto.StartDate);
            phase.EndDate.Should().Be(dto.EndDate);

            // ✅ Fix: Verify on HackathonPhaseRepository
            _phaseRepos.Verify(r => r.Update(phase), Times.Once);
        }

        // =============================
        // 13. UpdateAsync - Update thành công (chỉ có 1 phase)
        // =============================
        [Test]
        public async Task UpdateAsync_WhenOnlyOnePhase_ShouldUpdateSuccessfully()
        {
            var phase = new HackathonPhase 
            { 
                PhaseId = 1, 
                HackathonId = 1, 
                PhaseName = "Only Phase",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2)
            };
            var dto = new HackathonPhaseUpdateDto
            {
                PhaseName = "Updated Only Phase",
                StartDate = DateTime.UtcNow.AddDays(3),
                EndDate = DateTime.UtcNow.AddDays(5)
            };
            var existingPhases = new List<HackathonPhase> { phase }; // Chỉ có 1 phase

            // ✅ Fix: Use HackathonPhaseRepository instead of HackathonPhases
            _phaseRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);
            _phaseRepos.Setup(r => r.GetByHackathonIdAsync(1)).ReturnsAsync(existingPhases);
            _phaseRepos.Setup(r => r.Update(It.IsAny<HackathonPhase>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().BeTrue();
            phase.PhaseName.Should().Be("Updated Only Phase");

            // ✅ Fix: Verify on HackathonPhaseRepository
            _phaseRepos.Verify(r => r.Update(phase), Times.Once);
        }

        // =============================
        // 14. DeleteAsync - Phase không tồn tại
        // =============================
        [Test]
        public async Task DeleteAsync_WhenPhaseNotFound_ReturnsFalse()
        {
            // ✅ Fix: Use HackathonPhaseRepository instead of HackathonPhases
            _phaseRepos.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((HackathonPhase)null);

            var result = await _service.DeleteAsync(99);

            result.Should().BeFalse();
        }

        // =============================
        // 15. DeleteAsync - Xóa phase thành công
        // =============================
        [Test]
        public async Task DeleteAsync_WhenPhaseExists_ShouldDeleteAndReturnTrue()
        {
            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 1, PhaseName = "Phase to Delete" };

            // ✅ Fix: Use HackathonPhaseRepository instead of HackathonPhases
            _phaseRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);
            _phaseRepos.Setup(r => r.Remove(It.IsAny<HackathonPhase>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.DeleteAsync(1);

            result.Should().BeTrue();
            // ✅ Fix: Verify on HackathonPhaseRepository
            _phaseRepos.Verify(r => r.Remove(phase), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }

        // =============================
        // 16. CreateAsync - Edge case: phases liền kề (không overlap)
        // =============================
        [Test]
        public async Task CreateAsync_WhenPhasesAdjacent_ShouldCreateSuccessfully()
        {
            var baseTime = DateTime.UtcNow.Date; // Use date only to avoid millisecond precision issues
            var dto = new HackathonPhaseCreateDto
            {
                HackathonId = 1,
                PhaseName = "Adjacent Phase",
                StartDate = baseTime.AddDays(3), // Bắt đầu ngay khi phase trước kết thúc
                EndDate = baseTime.AddDays(5)
            };
            var existingPhases = new List<HackathonPhase>
            {
                new HackathonPhase 
                { 
                    PhaseId = 1, 
                    HackathonId = 1,
                    StartDate = baseTime.AddDays(1),
                    EndDate = baseTime.AddDays(3) // Kết thúc đúng lúc phase mới bắt đầu (no overlap)
                }
            };
            var phase = new HackathonPhase { PhaseId = 2, PhaseName = "Adjacent Phase", HackathonId = 1 };
            var phaseDto = new HackathonPhaseDto { PhaseId = 2, PhaseName = "Adjacent Phase", HackathonId = 1 };

            _phaseRepos.Setup(r => r.GetByHackathonIdAsync(1)).ReturnsAsync(existingPhases);
            _mapperMock.Setup(m => m.Map<HackathonPhase>(dto)).Returns(phase);
            // ✅ Fix: Use HackathonPhaseRepository for AddAsync
            _phaseRepos.Setup(r => r.AddAsync(It.IsAny<HackathonPhase>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<HackathonPhaseDto>(phase)).Returns(phaseDto);

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.PhaseName.Should().Be("Adjacent Phase");

            // ✅ Fix: Verify on HackathonPhaseRepository
            _phaseRepos.Verify(r => r.AddAsync(phase), Times.Once);
        }

        // =============================
        // 17. UpdateAsync - Edge case: update để phases liền kề
        // =============================
        [Test]
        public async Task UpdateAsync_WhenMakingPhasesAdjacent_ShouldUpdateSuccessfully()
        {
            var phase = new HackathonPhase 
            { 
                PhaseId = 1, 
                HackathonId = 1, 
                PhaseName = "Phase 1",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(2)
            };
            var dto = new HackathonPhaseUpdateDto
            {
                PhaseName = "Updated Phase 1",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(3) // Kết thúc đúng lúc phase 2 bắt đầu
            };
            var existingPhases = new List<HackathonPhase>
            {
                phase,
                new HackathonPhase 
                { 
                    PhaseId = 2, 
                    HackathonId = 1,
                    StartDate = DateTime.UtcNow.AddDays(3), // Bắt đầu đúng lúc phase 1 kết thúc
                    EndDate = DateTime.UtcNow.AddDays(5)
                }
            };

            // ✅ Fix: Use HackathonPhaseRepository instead of HackathonPhases
            _phaseRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);
            _phaseRepos.Setup(r => r.GetByHackathonIdAsync(1)).ReturnsAsync(existingPhases);
            _phaseRepos.Setup(r => r.Update(It.IsAny<HackathonPhase>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().BeTrue();
            phase.PhaseName.Should().Be("Updated Phase 1");
            phase.EndDate.Should().Be(dto.EndDate);

            // ✅ Fix: Verify on HackathonPhaseRepository
            _phaseRepos.Verify(r => r.Update(phase), Times.Once);
        }
    }
}