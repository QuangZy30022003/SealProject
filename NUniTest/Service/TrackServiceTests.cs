using AutoMapper;
using Common.DTOs.ChallengeDto;
using Common.DTOs.TrackDto;
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
    public class TrackServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private TrackService _service;

        private Mock<IRepository<Track>> _trackRepo;
        private Mock<IRepository<HackathonPhase>> _phaseRepo;
        private Mock<IRepository<Challenge>> _challengeRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _trackRepo = new Mock<IRepository<Track>>();
            _phaseRepo = new Mock<IRepository<HackathonPhase>>();
            _challengeRepo = new Mock<IRepository<Challenge>>();

            _uowMock.Setup(u => u.Tracks).Returns(_trackRepo.Object);
            _uowMock.Setup(u => u.HackathonPhases).Returns(_phaseRepo.Object);
            _uowMock.Setup(u => u.Challenges).Returns(_challengeRepo.Object);

            _service = new TrackService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // 1. GetTracksdAsync - Lấy tất cả tracks
        // =============================
        [Test]
        public async Task GetTracksdAsync_ShouldReturnAllTracks()
        {
            var tracks = new List<Track>
            {
                new Track { TrackId = 1, Name = "Web Development", Description = "Build web apps" },
                new Track { TrackId = 2, Name = "AI/ML", Description = "Machine learning projects" }
            };
            var trackDtos = new List<TrackRespone>
            {
                new TrackRespone { TrackId = 1, Name = "Web Development", Description = "Build web apps" },
                new TrackRespone { TrackId = 2, Name = "AI/ML", Description = "Machine learning projects" }
            };

            _trackRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Track, bool>>>(),
                It.IsAny<Expression<Func<Track, object>>>()))
                .ReturnsAsync(tracks);

            _mapperMock.Setup(m => m.Map<List<TrackRespone>>(tracks)).Returns(trackDtos);

            var result = await _service.GetTracksdAsync();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Web Development");
        }

        // =============================
        // 2. GetTrackByIdAsync - Track tồn tại
        // =============================
        [Test]
        public async Task GetTrackByIdAsync_WhenTrackExists_ShouldReturnTrack()
        {
            var tracks = new List<Track>
            {
                new Track { TrackId = 1, Name = "Web Development", Description = "Build web apps" }
            };
            var trackDto = new TrackRespone { TrackId = 1, Name = "Web Development", Description = "Build web apps" };

            _trackRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Track, bool>>>(),
                It.IsAny<Expression<Func<Track, object>>>()))
                .ReturnsAsync(tracks);

            _mapperMock.Setup(m => m.Map<TrackRespone>(tracks.First())).Returns(trackDto);

            var result = await _service.GetTrackByIdAsync(1);

            result.Should().NotBeNull();
            result.TrackId.Should().Be(1);
            result.Name.Should().Be("Web Development");
        }

        // =============================
        // 3. GetTrackByIdAsync - Track không tồn tại
        // =============================
        [Test]
        public async Task GetTrackByIdAsync_WhenTrackNotExists_ShouldReturnNull()
        {
            _trackRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Track, bool>>>(),
                It.IsAny<Expression<Func<Track, object>>>()))
                .ReturnsAsync(new List<Track>());

            var result = await _service.GetTrackByIdAsync(99);

            result.Should().BeNull();
        }

        // =============================
        // 4. CreateTrackAsync - Phase không tồn tại
        // =============================
        [Test]
        public void CreateTrackAsync_WhenPhaseNotFound_Throws()
        {
            var dto = new CreateTrackDto { Name = "New Track", Description = "Description", PhaseId = 99 };

            _phaseRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((HackathonPhase)null);

            Func<Task> act = async () => await _service.CreateTrackAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Phase not found");
        }

        // =============================
        // 5. CreateTrackAsync - Hackathon không có phases
        // =============================
        [Test]
        public void CreateTrackAsync_WhenHackathonHasNoPhases_Throws()
        {
            var dto = new CreateTrackDto { Name = "New Track", Description = "Description", PhaseId = 1 };
            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 1 };

            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);
            _phaseRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<HackathonPhase, bool>>>(), null, null))
                     .ReturnsAsync(new List<HackathonPhase>());

            Func<Task> act = async () => await _service.CreateTrackAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Hackathon does not contain any phases");
        }

        // =============================
        // 6. CreateTrackAsync - Không phải phase đầu tiên
        // =============================
        [Test]
        public void CreateTrackAsync_WhenNotFirstPhase_Throws()
        {
            var dto = new CreateTrackDto { Name = "New Track", Description = "Description", PhaseId = 2 };
            var phase = new HackathonPhase { PhaseId = 2, HackathonId = 1 };
            var phases = new List<HackathonPhase>
            {
                new HackathonPhase { PhaseId = 1, HackathonId = 1, StartDate = DateTime.UtcNow.AddDays(-1) }, // Phase đầu tiên
                new HackathonPhase { PhaseId = 2, HackathonId = 1, StartDate = DateTime.UtcNow } // Phase thứ hai
            };

            _phaseRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(phase);
            _phaseRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<HackathonPhase, bool>>>(), null, null))
                     .ReturnsAsync(phases);

            Func<Task> act = async () => await _service.CreateTrackAsync(dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Tracks can only be created for the first phase of this hackathon");
        }

        // =============================
        // 7. CreateTrackAsync - Tạo track thành công cho phase đầu tiên
        // =============================
        [Test]
        public async Task CreateTrackAsync_WhenValidFirstPhase_ShouldCreateTrack()
        {
            var dto = new CreateTrackDto
            {
                Name = "Web Development",
                Description = "Build web apps",
                PhaseId = 1
            };

            var phase = new HackathonPhase { PhaseId = 1, HackathonId = 1 };

            var phases = new List<HackathonPhase>
    {
        new HackathonPhase { PhaseId = 1, HackathonId = 1, StartDate = DateTime.UtcNow.AddDays(-1) },
        new HackathonPhase { PhaseId = 2, HackathonId = 1, StartDate = DateTime.UtcNow }
    };

            var trackDto = new TrackRespone { TrackId = 1, Name = "Web Development" };

            _phaseRepo.Setup(r => r.GetByIdAsync(1))
                      .ReturnsAsync(phase);

            _phaseRepo.Setup(r => r.GetAllAsync(
                        It.IsAny<Expression<Func<HackathonPhase, bool>>>(),
                        It.IsAny<Func<IQueryable<HackathonPhase>, IOrderedQueryable<HackathonPhase>>>(),
                        It.IsAny<string>()))
                      .ReturnsAsync(phases);

            _trackRepo.Setup(r => r.AddAsync(It.IsAny<Track>()))
                      .Returns(Task.CompletedTask);

            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>()))
                    .ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<TrackRespone>(It.IsAny<Track>()))
                       .Returns(trackDto);

            var result = await _service.CreateTrackAsync(dto);

            result.Should().NotBeNull();
            result.TrackId.Should().Be(1);
            result.Name.Should().Be("Web Development");

            _trackRepo.Verify(r => r.AddAsync(It.Is<Track>(t =>
                t.Name == "Web Development" &&
                t.Description == "Build web apps" &&
                t.PhaseId == 1)), Times.Once);
        }

        // =============================
        // 8. UpdateTrackAsync - Track không tồn tại
        // =============================
        [Test]
        public async Task UpdateTrackAsync_WhenTrackNotFound_ReturnsNull()
        {
            var dto = new UpdateTrackDto { Name = "Updated Track", Description = "Updated desc", PhaseId = 1 };

            _trackRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Track)null);

            var result = await _service.UpdateTrackAsync(99, dto);

            result.Should().BeNull();
        }

        // =============================
        // 9. UpdateTrackAsync - Update thành công
        // =============================
        [Test]
        public async Task UpdateTrackAsync_WhenValid_ShouldUpdateTrack()
        {
            var dto = new UpdateTrackDto { Name = "Updated Track", Description = "Updated description", PhaseId = 2 };
            var track = new Track { TrackId = 1, Name = "Old Track", Description = "Old desc", PhaseId = 1 };
            var trackDto = new TrackRespone { TrackId = 1, Name = "Updated Track" };

            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);
            _trackRepo.Setup(r => r.Update(It.IsAny<Track>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<TrackRespone>(track)).Returns(trackDto);

            var result = await _service.UpdateTrackAsync(1, dto);

            result.Should().NotBeNull();
            result.Name.Should().Be("Updated Track");

            track.Name.Should().Be("Updated Track");
            track.Description.Should().Be("Updated description");
            track.PhaseId.Should().Be(2);

            _trackRepo.Verify(r => r.Update(track), Times.Once);
        }

        // =============================
        // 10. DeleteTrackAsync - Track không tồn tại
        // =============================
        [Test]
        public async Task DeleteTrackAsync_WhenTrackNotFound_ReturnsFalse()
        {
            _trackRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Track)null);

            var result = await _service.DeleteTrackAsync(99);

            result.Should().BeFalse();
        }

        // =============================
        // 11. DeleteTrackAsync - Phase không tồn tại
        // =============================
        [Test]
        public void DeleteTrackAsync_WhenPhaseNotFound_Throws()
        {
            var track = new Track { TrackId = 1, PhaseId = 99 };

            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);
            _phaseRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((HackathonPhase)null);

            Func<Task> act = async () => await _service.DeleteTrackAsync(1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Phase not found for this track");
        }

        // =============================
        // 12. DeleteTrackAsync - Phase đã bắt đầu (có StartDate)
        // =============================
        [Test]
        public void DeleteTrackAsync_WhenPhaseStarted_Throws()
        {
            var track = new Track { TrackId = 1, PhaseId = 1 };
            var phase = new HackathonPhase 
            { 
                PhaseId = 1, 
                StartDate = DateTime.UtcNow.AddDays(-1) // Đã bắt đầu 1 ngày trước
            };

            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            Func<Task> act = async () => await _service.DeleteTrackAsync(1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("This track cannot be deleted because its phase has already started.");
        }

        // =============================
        // 13. DeleteTrackAsync - Phase đã kết thúc (không có StartDate nhưng có EndDate)
        // =============================
        [Test]
        public void DeleteTrackAsync_WhenPhaseEnded_Throws()
        {
            var track = new Track { TrackId = 1, PhaseId = 1 };
            var phase = new HackathonPhase 
            { 
                PhaseId = 1, 
                StartDate = null,
                EndDate = DateTime.UtcNow.AddDays(-1) // Đã kết thúc 1 ngày trước
            };

            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            Func<Task> act = async () => await _service.DeleteTrackAsync(1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("This track cannot be deleted because its phase has ended.");
        }

        // =============================
        // 14. DeleteTrackAsync - Xóa thành công (phase chưa bắt đầu)
        // =============================
        [Test]
        public async Task DeleteTrackAsync_WhenPhaseNotStarted_ShouldDeleteAndReturnTrue()
        {
            var track = new Track { TrackId = 1, PhaseId = 1 };
            var phase = new HackathonPhase 
            { 
                PhaseId = 1, 
                StartDate = DateTime.UtcNow.AddDays(1) // Sẽ bắt đầu 1 ngày sau
            };

            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            _trackRepo.Setup(r => r.Remove(It.IsAny<Track>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.DeleteTrackAsync(1);

            result.Should().BeTrue();
            _trackRepo.Verify(r => r.Remove(track), Times.Once);
        }

        // =============================
        // 15. DeleteTrackAsync - Xóa thành công (cả StartDate và EndDate đều null)
        // =============================
        [Test]
        public async Task DeleteTrackAsync_WhenNoDates_ShouldDeleteAndReturnTrue()
        {
            var track = new Track { TrackId = 1, PhaseId = 1 };
            var phase = new HackathonPhase 
            { 
                PhaseId = 1, 
                StartDate = null,
                EndDate = null
            };

            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);
            _phaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(phase);

            _trackRepo.Setup(r => r.Remove(It.IsAny<Track>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.DeleteTrackAsync(1);

            result.Should().BeTrue();
            _trackRepo.Verify(r => r.Remove(track), Times.Once);
        }

        // =============================
        // 16. AssignRandomChallengesToTrackAsync - Track không tồn tại
        // =============================
        [Test]
        public async Task AssignRandomChallengesToTrackAsync_WhenTrackNotFound_ReturnsNull()
        {
            var request = new RandomChallengeTrackRequest { TrackId = 99, ChallengeIds = new List<int> { 1, 2 }, Quantity = 1 };

            _trackRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Track)null);

            var result = await _service.AssignRandomChallengesToTrackAsync(request);

            result.Should().BeNull();
        }

        // =============================
        // 17. AssignRandomChallengesToTrackAsync - Không có challenge hợp lệ
        // =============================
        [Test]
        public async Task AssignRandomChallengesToTrackAsync_WhenNoChallengesAvailable_ReturnsNull()
        {
            var request = new RandomChallengeTrackRequest { TrackId = 1, ChallengeIds = new List<int> { 1, 2 }, Quantity = 1 };
            var track = new Track { TrackId = 1, PhaseId = 1 };

            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);

            // Tất cả challenges đã được sử dụng
            _challengeRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Challenge, bool>>>(),
                It.IsAny<Expression<Func<Challenge, object>>>()))
                .ReturnsAsync(new List<Challenge>
                {
                    new Challenge { ChallengeId = 1, TrackId = 2 }, // Đã được assign
                    new Challenge { ChallengeId = 2, TrackId = 3 }  // Đã được assign
                });

            // Không có challenge hợp lệ
            _challengeRepo.Setup(r => r.GetAllIncludingAsync(
                It.Is<Expression<Func<Challenge, bool>>>(expr => expr.ToString().Contains("Complete")),
                It.IsAny<Expression<Func<Challenge, object>>>()))
                .ReturnsAsync(new List<Challenge>());

            var result = await _service.AssignRandomChallengesToTrackAsync(request);

            result.Should().BeNull();
        }

        // =============================
        // 18. AssignRandomChallengesToTrackAsync - Assign thành công
        // =============================
        [Test]
        public async Task AssignRandomChallengesToTrackAsync_WhenValid_ShouldAssignChallenges()
        {
            var request = new RandomChallengeTrackRequest { TrackId = 1, ChallengeIds = new List<int> { 1, 2, 3 }, Quantity = 2 };
            var track = new Track { TrackId = 1, PhaseId = 1 };
            var availableChallenges = new List<Challenge>
            {
                new Challenge 
                { 
                    ChallengeId = 1, 
                    Title = "Challenge 1", 
                    Status = "Complete", 
                    TrackId = null,
                    User = new User { FullName = "User 1" }
                },
                new Challenge 
                { 
                    ChallengeId = 2, 
                    Title = "Challenge 2", 
                    Status = "Complete", 
                    TrackId = null,
                    User = new User { FullName = "User 2" }
                },
                new Challenge 
                { 
                    ChallengeId = 3, 
                    Title = "Challenge 3", 
                    Status = "Complete", 
                    TrackId = null,
                    User = new User { FullName = "User 3" }
                }
            };

            _trackRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(track);

            // Không có challenge nào đã được sử dụng
            _challengeRepo.Setup(r => r.GetAllIncludingAsync(
                It.Is<Expression<Func<Challenge, bool>>>(expr => expr.ToString().Contains("TrackId")),
                It.IsAny<Expression<Func<Challenge, object>>>()))
                .ReturnsAsync(new List<Challenge>());

            // Có challenges hợp lệ
            _challengeRepo.Setup(r => r.GetAllIncludingAsync(
                It.Is<Expression<Func<Challenge, bool>>>(expr => expr.ToString().Contains("Complete")),
                It.IsAny<Expression<Func<Challenge, object>>>()))
                .ReturnsAsync(availableChallenges);

            _challengeRepo.Setup(r => r.Update(It.IsAny<Challenge>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.AssignRandomChallengesToTrackAsync(request);

            result.Should().NotBeNull();
            result.TrackId.Should().Be(1);
            result.SelectedChallenges.Should().HaveCount(2); // Quantity = 2

            // Verify challenges được update với TrackId
            _challengeRepo.Verify(r => r.Update(It.IsAny<Challenge>()), Times.Exactly(2));
        }

        // =============================
        // 19. GetTracksByPhaseIdAsync - Lấy tracks theo phase
        // =============================
        [Test]
        public async Task GetTracksByPhaseIdAsync_ShouldReturnTracksByPhase()
        {
            var tracks = new List<Track>
            {
                new Track { TrackId = 1, Name = "Track 1", PhaseId = 1 },
                new Track { TrackId = 2, Name = "Track 2", PhaseId = 1 }
            };
            var trackDtos = new List<TrackRespone>
            {
                new TrackRespone { TrackId = 1, Name = "Track 1", PhaseId = 1 },
                new TrackRespone { TrackId = 2, Name = "Track 2", PhaseId = 1 }
            };

            _trackRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<Expression<Func<Track, bool>>>(),
                It.IsAny<Expression<Func<Track, object>>>()))
                .ReturnsAsync(tracks);

            _mapperMock.Setup(m => m.Map<List<TrackRespone>>(tracks)).Returns(trackDtos);

            var result = await _service.GetTracksByPhaseIdAsync(1);

            result.Should().HaveCount(2);
            result.All(t => t.PhaseId == 1).Should().BeTrue();
        }
    }
}