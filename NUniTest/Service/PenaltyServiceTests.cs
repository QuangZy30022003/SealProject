using AutoMapper;
using Common.DTOs.PenaltyBonusDto;
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
    public class PenaltyServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IScoreService> _scoreService;
        private PenaltyService _service;

        private Mock<IRepository<PenaltiesBonuse>> _penaltyRepo;
        private Mock<IRepository<Team>> _teamRepo;
        private Mock<IRepository<HackathonPhase>> _phaseRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _scoreService = new Mock<IScoreService>();

            _penaltyRepo = new Mock<IRepository<PenaltiesBonuse>>();
            _teamRepo = new Mock<IRepository<Team>>();
            _phaseRepo = new Mock<IRepository<HackathonPhase>>();

            _uowMock.Setup(u => u.PenaltiesBonuses).Returns(_penaltyRepo.Object);
            _uowMock.Setup(u => u.Teams).Returns(_teamRepo.Object);
            _uowMock.Setup(u => u.HackathonPhases).Returns(_phaseRepo.Object);

            _service = new PenaltyService(_uowMock.Object, _mapperMock.Object, _scoreService.Object);
        }

        // =============================
        // 1. CreateAsync - Team không tồn tại
        // =============================
        [Test]
        public void CreateAsync_WhenTeamNotFound_Throws()
        {
            var dto = new CreatePenaltiesBonuseDto { TeamId = 99, PhaseId = 1, Type = "Penalty", Points = 10 };

            _teamRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Team)null);

            Func<Task> act = async () => await _service.CreateAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team does not exist.");
        }

        // =============================
        // 2. CreateAsync - Phase không tồn tại
        // =============================
        [Test]
        public void CreateAsync_WhenPhaseNotFound_Throws()
        {
            var dto = new CreatePenaltiesBonuseDto { TeamId = 1, PhaseId = 99, Type = "Penalty", Points = 10 };
            var team = new Team { TeamId = 1, HackathonId = 1 };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _phaseRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((HackathonPhase)null);

            Func<Task> act = async () => await _service.CreateAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Phase does not exist.");
        }

        // =============================
        // 3. CreateAsync - Team không thuộc hackathon của phase
        // =============================
        [Test]
        public void CreateAsync_WhenTeamNotInPhaseHackathon_Throws()
        {
            var dto = new CreatePenaltiesBonuseDto { TeamId = 1, PhaseId = 1, Type = "Penalty", Points = 10 };
            var team = new Team { TeamId = 1, HackathonId = 1 };
            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 2 }; // Khác hackathon

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            Func<Task> act = async () => await _service.CreateAsync(dto, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Team does not belong to this hackathon phase.");
        }

        // =============================
        // 4. CreateAsync - Type không hợp lệ
        // =============================
        [Test]
        public void CreateAsync_WhenInvalidType_Throws()
        {
            var dto = new CreatePenaltiesBonuseDto { TeamId = 1, PhaseId = 1, Type = "Invalid", Points = 10 };
            var team = new Team { TeamId = 1, HackathonId = 1 };
            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 1 };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            Func<Task> act = async () => await _service.CreateAsync(dto, 1);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Type must be 'Penalty' or 'Bonus'.");
        }

        // =============================
        // 5. CreateAsync - Type rỗng
        // =============================
        [Test]
        public void CreateAsync_WhenEmptyType_Throws()
        {
            var dto = new CreatePenaltiesBonuseDto { TeamId = 1, PhaseId = 1, Type = "", Points = 10 };
            var team = new Team { TeamId = 1, HackathonId = 1 };
            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 1 };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            Func<Task> act = async () => await _service.CreateAsync(dto, 1);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Type is required (Penalty or Bonus).");
        }
        // =============================
        // 6. CreateAsync - Tạo Penalty thành công (points âm)
        // =============================
        [Test]
        public async Task CreateAsync_WhenValidPenalty_ShouldCreateWithNegativePoints()
        {
            var dto = new CreatePenaltiesBonuseDto 
            { 
                TeamId = 1, 
                PhaseId = 1, 
                Type = "Penalty", 
                Points = 10, // Sẽ được chuyển thành -10
                Reason = "Late submission"
            };
            var team = new Team { TeamId = 1, HackathonId = 1 };
            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 1 };
            var penalty = new PenaltiesBonuse { AdjustmentId = 1, TeamId = 1, Points = -10 };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            _penaltyRepo.Setup(r => r.AddAsync(It.IsAny<PenaltiesBonuse>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<PenaltiesBonuseResponseDto>(It.IsAny<PenaltiesBonuse>()))
                      .Returns(new PenaltiesBonuseResponseDto { AdjustmentId = 1, Points = -10 });

            var result = await _service.CreateAsync(dto, 5);

            result.Should().NotBeNull();
            result.Points.Should().Be(-10);

            _penaltyRepo.Verify(r => r.AddAsync(It.Is<PenaltiesBonuse>(p => 
                p.Points == -10 && 
                p.Type == "Penalty" && 
                p.CreatedBy == 5)), Times.Once);
        }

        // =============================
        // 7. CreateAsync - Tạo Bonus thành công (points dương)
        // =============================
        [Test]
        public async Task CreateAsync_WhenValidBonus_ShouldCreateWithPositivePoints()
        {
            var dto = new CreatePenaltiesBonuseDto 
            { 
                TeamId = 1, 
                PhaseId = 1, 
                Type = "Bonus", 
                Points = -5, // Sẽ được chuyển thành +5
                Reason = "Excellent presentation"
            };
            var team = new Team { TeamId = 1, HackathonId = 1 };
            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 1 };

            _teamRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(team);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            _penaltyRepo.Setup(r => r.AddAsync(It.IsAny<PenaltiesBonuse>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<PenaltiesBonuseResponseDto>(It.IsAny<PenaltiesBonuse>()))
                      .Returns(new PenaltiesBonuseResponseDto { AdjustmentId = 1, Points = 5 });

            var result = await _service.CreateAsync(dto, 5);

            result.Should().NotBeNull();
            result.Points.Should().Be(5);

            _penaltyRepo.Verify(r => r.AddAsync(It.Is<PenaltiesBonuse>(p => 
                p.Points == 5 && 
                p.Type == "Bonus" && 
                p.CreatedBy == 5)), Times.Once);
        }

        // =============================
        // 8. GetAllAsync - Lấy tất cả penalties/bonuses
        // =============================
        [Test]
        public async Task GetAllAsync_ShouldReturnAllPenalties()
        {
            var penalties = new List<PenaltiesBonuse>
            {
                new PenaltiesBonuse { AdjustmentId = 1, Type = "Penalty", Points = -10 },
                new PenaltiesBonuse { AdjustmentId = 2, Type = "Bonus", Points = 5 }
            };
            var penaltyDtos = new List<PenaltiesBonuseResponseDto>
            {
                new PenaltiesBonuseResponseDto { AdjustmentId = 1, Points = -10 },
                new PenaltiesBonuseResponseDto { AdjustmentId = 2, Points = 5 }
            };

            _penaltyRepo.Setup(r =>
        r.GetAllAsync(
            It.IsAny<Expression<Func<PenaltiesBonuse, bool>>>(),
            It.IsAny<Func<IQueryable<PenaltiesBonuse>, IOrderedQueryable<PenaltiesBonuse>>>(),
            It.IsAny<string>()
        )
    )
    .ReturnsAsync(penalties);


            _mapperMock.Setup(m => m.Map<IEnumerable<PenaltiesBonuseResponseDto>>(penalties))
                      .Returns(penaltyDtos);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().AdjustmentId.Should().Be(1);
        }

        // =============================
        // 9. GetByPhaseAsync - Lấy penalties theo phase
        // =============================
        [Test]
        public async Task GetByPhaseAsync_ShouldReturnPenaltiesByPhase()
        {
            var penalties = new List<PenaltiesBonuse>
            {
                new PenaltiesBonuse { AdjustmentId = 1, PhaseId = 1, IsDeleted = false },
                new PenaltiesBonuse { AdjustmentId = 2, PhaseId = 1, IsDeleted = false }
            };
            var penaltyDtos = new List<PenaltiesBonuseResponseDto>
            {
                new PenaltiesBonuseResponseDto { AdjustmentId = 1, PhaseId = 1 },
                new PenaltiesBonuseResponseDto { AdjustmentId = 2, PhaseId = 1 }
            };

            _penaltyRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<PenaltiesBonuse, bool>>>(),
                It.IsAny<Func<IQueryable<PenaltiesBonuse>, IOrderedQueryable<PenaltiesBonuse>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(penalties);

            _mapperMock.Setup(m => m.Map<IEnumerable<PenaltiesBonuseResponseDto>>(penalties))
                      .Returns(penaltyDtos);

            var result = await _service.GetByPhaseAsync(1);

            result.Should().HaveCount(2);
            result.All(p => p.PhaseId == 1).Should().BeTrue();
        }

        // =============================
        // 10. GetByTeamAsync - Lấy penalties theo team và phase
        // =============================
        [Test]
        public async Task GetByTeamAsync_ShouldReturnPenaltiesByTeamAndPhase()
        {
            var penalties = new List<PenaltiesBonuse>
            {
                new PenaltiesBonuse { AdjustmentId = 1, TeamId = 1, PhaseId = 1, IsDeleted = false }
            };
            var penaltyDtos = new List<PenaltiesBonuseResponseDto>
            {
                new PenaltiesBonuseResponseDto { AdjustmentId = 1, TeamId = 1, PhaseId = 1 }
            };

            _penaltyRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<PenaltiesBonuse, bool>>>(),
                It.IsAny<Func<IQueryable<PenaltiesBonuse>, IOrderedQueryable<PenaltiesBonuse>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(penalties);

            _mapperMock.Setup(m => m.Map<IEnumerable<PenaltiesBonuseResponseDto>>(penalties))
                      .Returns(penaltyDtos);

            var result = await _service.GetByTeamAsync(1, 1);

            result.Should().HaveCount(1);
            result.First().TeamId.Should().Be(1);
            result.First().PhaseId.Should().Be(1);
        }
        // =============================
        // 11. GetByIdAsync - Lấy penalty theo ID
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenExists_ShouldReturnPenalty()
        {
            var penalty = new PenaltiesBonuse { AdjustmentId = 1, TeamId = 1, Points = -10 };
            var penaltyDto = new PenaltiesBonuseResponseDto { AdjustmentId = 1, TeamId = 1, Points = -10 };

            _penaltyRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<PenaltiesBonuse, bool>>>(),
                It.IsAny<Expression<Func<PenaltiesBonuse, object>>>(),
                It.IsAny<Expression<Func<PenaltiesBonuse, object>>>()))
                .ReturnsAsync(penalty);

            _mapperMock.Setup(m => m.Map<PenaltiesBonuseResponseDto>(penalty))
                      .Returns(penaltyDto);

            var result = await _service.GetByIdAsync(1);

            result.Should().NotBeNull();
            result.AdjustmentId.Should().Be(1);
        }

        [Test]
        public async Task GetByIdAsync_WhenNotExists_ShouldReturnNull()
        {
            _penaltyRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<PenaltiesBonuse, bool>>>(),
                It.IsAny<Expression<Func<PenaltiesBonuse, object>>>(),
                It.IsAny<Expression<Func<PenaltiesBonuse, object>>>()))
                .ReturnsAsync((PenaltiesBonuse)null);

            var result = await _service.GetByIdAsync(99);

            result.Should().BeNull();
        }

        // =============================
        // 12. UpdateAsync - Penalty không tồn tại
        // =============================
        [Test]
        public async Task UpdateAsync_WhenPenaltyNotFound_ReturnsNull()
        {
            var dto = new UpdatePenaltiesBonuseDto { Points = 15, Reason = "Updated reason" };

            _penaltyRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PenaltiesBonuse)null);

            var result = await _service.UpdateAsync(99, dto);

            result.Should().BeNull();
        }

        // =============================
        // 13. UpdateAsync - Penalty đã bị xóa
        // =============================
        [Test]
        public async Task UpdateAsync_WhenPenaltyDeleted_ReturnsNull()
        {
            var dto = new UpdatePenaltiesBonuseDto { Points = 15, Reason = "Updated reason" };
            var penalty = new PenaltiesBonuse { AdjustmentId = 1, IsDeleted = true };

            _penaltyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(penalty);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().BeNull();
        }

        // =============================
        // 14. UpdateAsync - Update Penalty thành công (giữ points âm)
        // =============================
        [Test]
        public async Task UpdateAsync_WhenValidPenalty_ShouldUpdateWithNegativePoints()
        {
            var dto = new UpdatePenaltiesBonuseDto { Points = 15, Reason = "Updated penalty reason" };
            var penalty = new PenaltiesBonuse 
            { 
                AdjustmentId = 1, 
                Type = "Penalty", 
                Points = -10, 
                Reason = "Original reason",
                IsDeleted = false
            };

            _penaltyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(penalty);
            _penaltyRepo.Setup(r => r.Update(It.IsAny<PenaltiesBonuse>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _penaltyRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<PenaltiesBonuse, bool>>>(),
                It.IsAny<Expression<Func<PenaltiesBonuse, object>>>(),
                It.IsAny<Expression<Func<PenaltiesBonuse, object>>>()))
                .ReturnsAsync(penalty);

            _mapperMock.Setup(m => m.Map<PenaltiesBonuseResponseDto>(penalty))
                      .Returns(new PenaltiesBonuseResponseDto { AdjustmentId = 1, Points = -15 });

            var result = await _service.UpdateAsync(1, dto);

            result.Should().NotBeNull();
            penalty.Points.Should().Be(-15); // Points được chuyển thành âm cho Penalty
            penalty.Reason.Should().Be("Updated penalty reason");

            _penaltyRepo.Verify(r => r.Update(penalty), Times.Once);
        }

        // =============================
        // 15. UpdateAsync - Update Bonus thành công (giữ points dương)
        // =============================
        [Test]
        public async Task UpdateAsync_WhenValidBonus_ShouldUpdateWithPositivePoints()
        {
            var dto = new UpdatePenaltiesBonuseDto { Points = -20, Reason = "Updated bonus reason" };
            var bonus = new PenaltiesBonuse 
            { 
                AdjustmentId = 1, 
                Type = "Bonus", 
                Points = 5, 
                Reason = "Original reason",
                IsDeleted = false
            };

            _penaltyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bonus);
            _penaltyRepo.Setup(r => r.Update(It.IsAny<PenaltiesBonuse>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _penaltyRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<Expression<Func<PenaltiesBonuse, bool>>>(),
                It.IsAny<Expression<Func<PenaltiesBonuse, object>>>(),
                It.IsAny<Expression<Func<PenaltiesBonuse, object>>>()))
                .ReturnsAsync(bonus);

            _mapperMock.Setup(m => m.Map<PenaltiesBonuseResponseDto>(bonus))
                      .Returns(new PenaltiesBonuseResponseDto { AdjustmentId = 1, Points = 20 });

            var result = await _service.UpdateAsync(1, dto);

            result.Should().NotBeNull();
            bonus.Points.Should().Be(20); // Points được chuyển thành dương cho Bonus
            bonus.Reason.Should().Be("Updated bonus reason");

            _penaltyRepo.Verify(r => r.Update(bonus), Times.Once);
        }

        // =============================
        // 16. SoftDeleteAsync - Penalty không tồn tại
        // =============================
        [Test]
        public async Task SoftDeleteAsync_WhenPenaltyNotFound_ReturnsFalse()
        {
            _penaltyRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PenaltiesBonuse)null);

            var result = await _service.SoftDeleteAsync(99);

            result.Should().BeFalse();
        }

        // =============================
        // 17. SoftDeleteAsync - Penalty đã bị xóa
        // =============================
        [Test]
        public async Task SoftDeleteAsync_WhenPenaltyAlreadyDeleted_ReturnsFalse()
        {
            var penalty = new PenaltiesBonuse { AdjustmentId = 1, IsDeleted = true };

            _penaltyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(penalty);

            var result = await _service.SoftDeleteAsync(1);

            result.Should().BeFalse();
        }

        // =============================
        // 18. SoftDeleteAsync - Xóa thành công
        // =============================
        [Test]
        public async Task SoftDeleteAsync_WhenValid_ShouldSoftDelete()
        {
            var penalty = new PenaltiesBonuse { AdjustmentId = 1, IsDeleted = false };

            _penaltyRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(penalty);
            _penaltyRepo.Setup(r => r.Update(It.IsAny<PenaltiesBonuse>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.SoftDeleteAsync(1);

            result.Should().BeTrue();
            penalty.IsDeleted.Should().BeTrue();
            penalty.UpdatedAt.Should().NotBeNull();

            _penaltyRepo.Verify(r => r.Update(penalty), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }
    }
}