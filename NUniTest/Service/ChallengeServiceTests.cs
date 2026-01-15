using AutoMapper;
using Common.DTOs.ChallengeDto;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
    public class ChallengeServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IFileUploadService> _fileUploadServiceMock;
        private ChallengeService _service;

        private Mock<IRepository<Challenge>> _challengeRepo;
        private Mock<IChallengeRepository> _challengeRepos;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _fileUploadServiceMock = new Mock<IFileUploadService>();

            _challengeRepo = new Mock<IRepository<Challenge>>();
            _challengeRepos = new Mock<IChallengeRepository>();

            _uowMock.Setup(u => u.Challenges).Returns(_challengeRepo.Object);
            _uowMock.Setup(u => u.ChallengeRepository).Returns(_challengeRepos.Object);

            _service = new ChallengeService(_uowMock.Object, _mapperMock.Object, _fileUploadServiceMock.Object);
        }

        // =============================
        // 1. GetAllAsync - Lấy tất cả challenges
        // =============================
        [Test]
        public async Task GetAllAsync_ShouldReturnAllChallenges()
        {
            var challenges = new List<Challenge>
            {
                new Challenge { ChallengeId = 1, Title = "Challenge 1", Status = "Pending" },
                new Challenge { ChallengeId = 2, Title = "Challenge 2", Status = "Approved" }
            };
            var challengeDtos = new List<ChallengeDto>
            {
                new ChallengeDto { ChallengeId = 1, Title = "Challenge 1", Status = "Pending" },
                new ChallengeDto { ChallengeId = 2, Title = "Challenge 2", Status = "Approved" }
            };

            _challengeRepos
    .Setup(r => r.GetAllIncludingAsync(
        It.IsAny<Expression<Func<Challenge, bool>>?>(),
        It.IsAny<Expression<Func<Challenge, object>>[]>()
    ))
    .ReturnsAsync(challenges);


            _mapperMock.Setup(m => m.Map<IEnumerable<ChallengeDto>>(challenges))
                       .Returns(challengeDtos);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().Title.Should().Be("Challenge 1");
        }

        // =============================
        // 2. GetByIdAsync - Challenge tồn tại
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenChallengeExists_ShouldReturnChallenge()
        {
            var challenge = new Challenge { ChallengeId = 1, Title = "Test Challenge" };
            var challengeDto = new ChallengeDto { ChallengeId = 1, Title = "Test Challenge" };

            // ✅ Fix: Use correct repository (ChallengeRepository instead of Challenges)
            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);
            _mapperMock.Setup(m => m.Map<ChallengeDto>(challenge)).Returns(challengeDto);

            var result = await _service.GetByIdAsync(1);

            result.Should().NotBeNull();
            result!.ChallengeId.Should().Be(1);
            result.Title.Should().Be("Test Challenge");
        }

        // =============================
        // 3. GetByIdAsync - Challenge không tồn tại
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenChallengeNotExists_ShouldReturnNull()
        {
            // ✅ Fix: Use correct repository (ChallengeRepository instead of Challenges)
            _challengeRepos.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Challenge)null);

            var result = await _service.GetByIdAsync(99);

            result.Should().BeNull();
        }

        // =============================
        // 4. CreateAsync - Không có file
        // =============================
        [Test]
        public void CreateAsync_WhenNoFile_Throws()
        {
            var dto = new ChallengeCreateUnifiedDto 
            { 
                Title = "Test Challenge", 
                Description = "Test Description", 
                HackathonId = 1,
                File = null 
            };

            Func<Task> act = async () => await _service.CreateAsync(dto, 1);

            act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("You must provide a file");
        }

        // =============================
        // 5. CreateAsync - Tạo challenge thành công
        // =============================
        [Test]
        public async Task CreateAsync_WhenValid_ShouldCreateChallenge()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            var dto = new ChallengeCreateUnifiedDto
            {
                Title = "AI Challenge",
                Description = "Build an AI model",
                HackathonId = 1,
                File = mockFile.Object
            };

            var fileUrl = "https://cloudinary.com/test.pdf";
            var challengeDto = new ChallengeDto { ChallengeId = 1, Title = "AI Challenge" };

            // file upload returns url
            _fileUploadServiceMock.Setup(f => f.UploadAsync(mockFile.Object)).ReturnsAsync(fileUrl);

            // AddAsync on repository
            _challengeRepos.Setup(r => r.AddAsync(It.IsAny<Challenge>())).Returns(Task.CompletedTask);

            _mapperMock.Setup(m => m.Map<ChallengeDto>(It.IsAny<Challenge>())).Returns(challengeDto);

            var result = await _service.CreateAsync(dto, 5);

            result.Should().NotBeNull();
            result.ChallengeId.Should().Be(1);

            // Verify repository Add called with expected values
            _challengeRepos.Verify(r => r.AddAsync(It.Is<Challenge>(c =>
                c.Title == "AI Challenge" &&
                c.HackathonId == 1 &&
                c.UserId == 5 &&
                c.Status == "Pending" &&
                c.FilePath == fileUrl)), Times.Once);

            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce);
        }

        // =============================
        // 6. PartnerDeleteAsync - Challenge không tồn tại
        // =============================
        [Test]
        public async Task PartnerDeleteAsync_WhenChallengeNotFound_ReturnsErrorMessage()
        {
            // ✅ Fix: Use correct repository (ChallengeRepository instead of Challenges)
            _challengeRepos.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Challenge)null);

            var result = await _service.PartnerDeleteAsync(99, 1);

            result.Should().Be("Challenge not found");
        }

        // =============================
        // 7. PartnerDeleteAsync - User không phải owner
        // =============================
        [Test]
        public async Task PartnerDeleteAsync_WhenUserNotOwner_ReturnsErrorMessage()
        {
            var challenge = new Challenge { ChallengeId = 1, UserId = 5, Status = "Pending" };
            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            var result = await _service.PartnerDeleteAsync(1, 10);

            result.Should().Be("You do not have permission to delete this challenge");
        }

        // =============================
        // 8. PartnerDeleteAsync - Challenge đã Complete hoặc Cancel
        // =============================
        [Test]
        public async Task PartnerDeleteAsync_WhenChallengeCompleted_ReturnsErrorMessage()
        {
            var challenge = new Challenge { ChallengeId = 1, UserId = 5, Status = "Complete" };
            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            var result = await _service.PartnerDeleteAsync(1, 5);

            result.Should().Be("Cannot delete challenge with status 'Complete'");
        }

        [Test]
        public async Task PartnerDeleteAsync_WhenChallengeCancelled_ReturnsErrorMessage()
        {
            var challenge = new Challenge { ChallengeId = 1, UserId = 5, Status = "Cancel" };
            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            var result = await _service.PartnerDeleteAsync(1, 5);

            result.Should().Be("Cannot delete challenge with status 'Cancel'");
        }
        // =============================
        // 9. PartnerDeleteAsync - Xóa thành công
        // =============================
        [Test]
        public async Task PartnerDeleteAsync_WhenValid_ShouldDeleteAndReturnNull()
        {
            var challenge = new Challenge { ChallengeId = 1, UserId = 5, Status = "Pending" };

            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);
            _challengeRepos.Setup(r => r.Remove(It.IsAny<Challenge>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.PartnerDeleteAsync(1, 5);

            result.Should().BeNull();
            _challengeRepos.Verify(r => r.Remove(challenge), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Once);
        }

        // =============================
        // 10. ChangeStatusAsync - Challenge không tồn tại
        // =============================
        [Test]
        public async Task ChangeStatusAsync_WhenChallengeNotFound_ReturnsFalse()
        {
            var statusDto = new ChallengeStatusDto { Status = "Approved" };

            // ✅ Fix: Use correct repository (ChallengeRepository instead of Challenges)
            _challengeRepos.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Challenge)null);

            var result = await _service.ChangeStatusAsync(99, statusDto);

            result.Should().BeFalse();
        }

        // =============================
        // 11. ChangeStatusAsync - Thay đổi status thành công
        // =============================
        [Test]
        public async Task ChangeStatusAsync_WhenValid_ShouldUpdateStatusAndReturnTrue()
        {
            var challenge = new Challenge { ChallengeId = 1, Status = "Pending" };
            var statusDto = new ChallengeStatusDto { Status = "Approved" };

            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);
            _challengeRepos.Setup(r => r.Update(It.IsAny<Challenge>()));

            var result = await _service.ChangeStatusAsync(1, statusDto);

            result.Should().BeTrue();
            challenge.Status.Should().Be("Approved");
            _challengeRepos.Verify(r => r.Update(challenge), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Once);
        }

        // =============================
        // 12. PartnerUpdateAsync - Challenge không tồn tại
        // =============================
        [Test]
        public async Task PartnerUpdateAsync_WhenChallengeNotFound_ReturnsErrorMessage()
        {
            var dto = new ChallengePartnerUpdateDto { Title = "Updated", Description = "Updated desc", HackathonId = 1 };

            // ✅ Fix: Use correct repository (ChallengeRepository instead of Challenges)
            _challengeRepos.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Challenge)null);

            var result = await _service.PartnerUpdateAsync(99, 1, dto);

            result.Should().Be("Challenge not found");
        }

        // =============================
        // 13. PartnerUpdateAsync - User không phải owner
        // =============================
        [Test]
        public async Task PartnerUpdateAsync_WhenUserNotOwner_ReturnsErrorMessage()
        {
            var challenge = new Challenge { ChallengeId = 1, UserId = 5, Status = "Pending" };
            var dto = new ChallengePartnerUpdateDto { Title = "Updated", Description = "Updated desc", HackathonId = 1 };

            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            var result = await _service.PartnerUpdateAsync(1, 10, dto);

            result.Should().Be("You are not the owner of this challenge");
        }

        // =============================
        // 14. PartnerUpdateAsync - Challenge đã Complete hoặc Cancel
        // =============================
        [Test]
        public async Task PartnerUpdateAsync_WhenChallengeCompleted_ReturnsErrorMessage()
        {
            var challenge = new Challenge { ChallengeId = 1, UserId = 5, Status = "Complete" };
            var dto = new ChallengePartnerUpdateDto { Title = "Updated", Description = "Updated desc", HackathonId = 1 };

            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            var result = await _service.PartnerUpdateAsync(1, 5, dto);

            result.Should().Be("Challenge is Complete or Cancelled, cannot update");
        }

        // =============================
        // 15. PartnerUpdateAsync - Không có file (FilePath rỗng)
        // =============================
        [Test]
        public async Task PartnerUpdateAsync_WhenNoFile_ReturnsErrorMessage()
        {
            var challenge = new Challenge
            {
                ChallengeId = 1,
                UserId = 5,
                Status = "Pending",
                FilePath = null
            };
            var dto = new ChallengePartnerUpdateDto
            {
                Title = "Updated",
                Description = "Updated desc",
                HackathonId = 1,
                File = null
            };

            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);

            var result = await _service.PartnerUpdateAsync(1, 5, dto);

            result.Should().Be("Challenge must have a file");
        }

        // =============================
        // 16. PartnerUpdateAsync - Update thành công với file mới
        // =============================
        [Test]
        public async Task PartnerUpdateAsync_WhenValidWithNewFile_ShouldUpdateAndReturnNull()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("updated.pdf");

            var challenge = new Challenge
            {
                ChallengeId = 1,
                UserId = 5,
                Status = "Pending",
                FilePath = "old_file.pdf",
                Title = "Old Title"
            };
            var dto = new ChallengePartnerUpdateDto
            {
                Title = "Updated Title",
                Description = "Updated Description",
                HackathonId = 2,
                File = mockFile.Object
            };
            var newFileUrl = "https://cloudinary.com/updated.pdf";

            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);
            _fileUploadServiceMock.Setup(f => f.UploadAsync(mockFile.Object)).ReturnsAsync(newFileUrl);
            _challengeRepos.Setup(r => r.Update(It.IsAny<Challenge>()));

            var result = await _service.PartnerUpdateAsync(1, 5, dto);

            result.Should().BeNull();
            challenge.Title.Should().Be("Updated Title");
            challenge.Description.Should().Be("Updated Description");
            challenge.HackathonId.Should().Be(2);
            challenge.FilePath.Should().Be(newFileUrl);

            _challengeRepos.Verify(r => r.Update(challenge), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Once);
        }

        // =============================
        // 17. PartnerUpdateAsync - Update thành công không có file mới
        // =============================
        [Test]
        public async Task PartnerUpdateAsync_WhenValidWithoutNewFile_ShouldUpdateAndReturnNull()
        {
            var challenge = new Challenge
            {
                ChallengeId = 1,
                UserId = 5,
                Status = "Pending",
                FilePath = "existing_file.pdf",
                Title = "Old Title"
            };
            var dto = new ChallengePartnerUpdateDto
            {
                Title = "Updated Title",
                Description = "Updated Description",
                HackathonId = 2,
                File = null
            };

            _challengeRepos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(challenge);
            _challengeRepos.Setup(r => r.Update(It.IsAny<Challenge>()));

            var result = await _service.PartnerUpdateAsync(1, 5, dto);

            result.Should().BeNull();
            challenge.Title.Should().Be("Updated Title");
            challenge.Description.Should().Be("Updated Description");
            challenge.HackathonId.Should().Be(2);
            challenge.FilePath.Should().Be("existing_file.pdf");

            _challengeRepos.Verify(r => r.Update(challenge), Times.Once);
            _fileUploadServiceMock.Verify(f => f.UploadAsync(It.IsAny<IFormFile>()), Times.Never);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Once);
        }

        // =============================
        // 18. GetCompletedChallengesByHackathonAsync - Lấy challenges completed chưa được assign
        // =============================
        [Test]
        public async Task GetCompletedChallengesByHackathonAsync_ShouldReturnUnassignedChallenges()
        {
            var challenges = new List<Challenge>
            {
                new Challenge { ChallengeId = 1, HackathonId = 1, Status = "Complete", TrackId = null }, // Chưa assign
                new Challenge { ChallengeId = 2, HackathonId = 1, Status = "Complete", TrackId = 1 }, // Đã assign
                new Challenge { ChallengeId = 3, HackathonId = 1, Status = "Complete", TrackId = null } // Chưa assign
            };
            var challengeDtos = new List<ChallengeDto>
            {
                new ChallengeDto { ChallengeId = 1, HackathonId = 1 },
                new ChallengeDto { ChallengeId = 3, HackathonId = 1 }
            };

            _challengeRepos.Setup(r => r.GetCompletedChallengesByHackathonIdAsync(1))
                         .ReturnsAsync(challenges);

            _mapperMock.Setup(m => m.Map<List<ChallengeDto>>(It.IsAny<List<Challenge>>()))
                      .Returns(challengeDtos);

            var result = await _service.GetCompletedChallengesByHackathonAsync(1);

            result.Should().HaveCount(2); // Chỉ những challenge chưa được assign (TrackId = null)
            result.All(c => c.HackathonId == 1).Should().BeTrue();
        }

        // =============================
        // 19. GetChallengesByTrackIdAsync - Lấy challenges theo track
        // =============================
        [Test]
        public async Task GetChallengesByTrackIdAsync_ShouldReturnChallengesByTrack()
        {
            var challenges = new List<Challenge>
            {
                new Challenge { ChallengeId = 1, TrackId = 1, Title = "Track 1 Challenge 1" },
                new Challenge { ChallengeId = 2, TrackId = 1, Title = "Track 1 Challenge 2" }
            };
            var challengeDtos = new List<ChallengeDto>
            {
                new ChallengeDto { ChallengeId = 1, Title = "Track 1 Challenge 1" },
                new ChallengeDto { ChallengeId = 2, Title = "Track 1 Challenge 2" }
            };

            // service calls ChallengeRepository.GetAllIncludingAsync with predicate and include
            _challengeRepos.Setup(r => r.GetAllIncludingAsync(It.IsAny<Expression<Func<Challenge, bool>>>(),
                                                              It.IsAny<Expression<Func<Challenge, object>>[]>()))
                           .ReturnsAsync(challenges);

            _mapperMock.Setup(m => m.Map<List<ChallengeDto>>(challenges)).Returns(challengeDtos);

            var result = await _service.GetChallengesByTrackIdAsync(1);

            result.Should().HaveCount(2);
            result.All(c => c.Title.Contains("Track 1")).Should().BeTrue();
        }

        // =============================
        // 20. GetMyChallengesByHackathonAsync - Lấy challenges của user theo hackathon
        // =============================
        [Test]
        public async Task GetMyChallengesByHackathonAsync_ShouldReturnUserChallenges()
        {
            var challenges = new List<Challenge>
            {
                new Challenge { ChallengeId = 1, UserId = 5, HackathonId = 1, Title = "My Challenge 1" },
                new Challenge { ChallengeId = 2, UserId = 5, HackathonId = 1, Title = "My Challenge 2" }
            };
            var challengeDtos = new List<ChallengeDto>
            {
                new ChallengeDto { ChallengeId = 1, Title = "My Challenge 1" },
                new ChallengeDto { ChallengeId = 2, Title = "My Challenge 2" }
            };

            _challengeRepos.Setup(r => r.GetAllIncludingAsync(It.IsAny<Expression<Func<Challenge, bool>>>(),
                                                              It.IsAny<Expression<Func<Challenge, object>>[]>()))
                           .ReturnsAsync(challenges);

            _mapperMock.Setup(m => m.Map<List<ChallengeDto>>(challenges)).Returns(challengeDtos);

            var result = await _service.GetMyChallengesByHackathonAsync(5, 1);

            result.Should().HaveCount(2);
            result.All(c => c.Title.Contains("My Challenge")).Should().BeTrue();
        }
    }
}