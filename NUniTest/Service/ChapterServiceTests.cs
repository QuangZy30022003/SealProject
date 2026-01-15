using AutoMapper;
using Common.DTOs.ChapterDto;
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
    public class ChapterServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private ChapterService _service;

        private Mock<IRepository<Chapter>> _chapterRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();

            _chapterRepo = new Mock<IRepository<Chapter>>();

            _uowMock.Setup(u => u.Chapters).Returns(_chapterRepo.Object);

            _service = new ChapterService(_uowMock.Object, _mapperMock.Object);
        }

        // =============================
        // 1. CreateChapterAsync - Chapter name already exists
        // =============================
        [Test]
        public void CreateChapterAsync_WhenChapterNameExists_Throws()
        {
            var dto = new CreateChapterDto
            {
                ChapterName = "Existing Chapter",
                Description = "Test description"
            };

            _chapterRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, bool>>>()))
                .ReturnsAsync(true);

            Func<Task> act = async () => await _service.CreateChapterAsync(dto, 1);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Chapter name already exists");
        }

        // =============================
        // 2. CreateChapterAsync - Successful creation
        // =============================
        [Test]
        public async Task CreateChapterAsync_WhenValidData_ShouldCreateSuccessfully()
        {
            var dto = new CreateChapterDto
            {
                ChapterName = "New Chapter",
                Description = "Test description"
            };
            var entity = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "New Chapter", 
                Description = "Test description",
                ChapterLeaderId = 1
            };
            var createdEntity = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "New Chapter", 
                Description = "Test description",
                ChapterLeaderId = 1,
                ChapterLeader = new User { UserId = 1, FullName = "Leader Name" }
            };
            var chapterDto = new ChapterDto 
            { 
                ChapterId = 1, 
                ChapterName = "New Chapter", 
                Description = "Test description",
                ChapterLeaderId = 1,
                ChapterLeaderName = "Leader Name"
            };

            _chapterRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, bool>>>()))
                .ReturnsAsync(false);
            _mapperMock.Setup(m => m.Map<Chapter>(dto)).Returns(entity);
            _chapterRepo.Setup(r => r.AddAsync(It.IsAny<Chapter>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _chapterRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, object>>[]>()))
                .ReturnsAsync(createdEntity);
            _mapperMock.Setup(m => m.Map<ChapterDto>(createdEntity)).Returns(chapterDto);

            var result = await _service.CreateChapterAsync(dto, 1);

            result.Should().NotBeNull();
            result.ChapterName.Should().Be("New Chapter");
            result.ChapterLeaderId.Should().Be(1);
            result.ChapterLeaderName.Should().Be("Leader Name");

            _chapterRepo.Verify(r => r.AddAsync(It.IsAny<Chapter>()), Times.Once);
            entity.ChapterLeaderId.Should().Be(1);
        }

        // =============================
        // 3. GetByIdAsync - Chapter exists
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenChapterExists_ShouldReturnChapter()
        {
            var chapter = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "Test Chapter",
                ChapterLeader = new User { UserId = 1, FullName = "Leader Name" }
            };
            var chapterDto = new ChapterDto 
            { 
                ChapterId = 1, 
                ChapterName = "Test Chapter",
                ChapterLeaderName = "Leader Name"
            };

            _chapterRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, object>>[]>()))
                .ReturnsAsync(chapter);
            _mapperMock.Setup(m => m.Map<ChapterDto>(chapter)).Returns(chapterDto);

            var result = await _service.GetByIdAsync(1);

            result.Should().NotBeNull();
            result.ChapterName.Should().Be("Test Chapter");
            result.ChapterLeaderName.Should().Be("Leader Name");
        }

        // =============================
        // 4. GetByIdAsync - Chapter not exists
        // =============================
        [Test]
        public async Task GetByIdAsync_WhenChapterNotExists_ShouldReturnNull()
        {
            _chapterRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, object>>[]>()))
                .ReturnsAsync((Chapter)null);

            var result = await _service.GetByIdAsync(99);

            result.Should().BeNull();
        }

        // =============================
        // 5. GetAllAsync - Return all chapters
        // =============================
        [Test]
        public async Task GetAllAsync_ShouldReturnAllChapters()
        {
            var chapters = new List<Chapter>
            {
                new Chapter 
                { 
                    ChapterId = 1, 
                    ChapterName = "Chapter 1",
                    ChapterLeader = new User { UserId = 1, FullName = "Leader 1" }
                },
                new Chapter 
                { 
                    ChapterId = 2, 
                    ChapterName = "Chapter 2",
                    ChapterLeader = new User { UserId = 2, FullName = "Leader 2" }
                }
            };
            var chapterDtos = new List<ChapterDto>
            {
                new ChapterDto { ChapterId = 1, ChapterName = "Chapter 1", ChapterLeaderName = "Leader 1" },
                new ChapterDto { ChapterId = 2, ChapterName = "Chapter 2", ChapterLeaderName = "Leader 2" }
            };

            _chapterRepo.Setup(r => r.GetAllIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, object>>[]>()))
                .ReturnsAsync(chapters);
            _mapperMock.Setup(m => m.Map<IEnumerable<ChapterDto>>(chapters)).Returns(chapterDtos);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().ChapterName.Should().Be("Chapter 1");
            result.Last().ChapterName.Should().Be("Chapter 2");
        }

        // =============================
        // 6. UpdateAsync - Chapter not found
        // =============================
        [Test]
        public async Task UpdateAsync_WhenChapterNotFound_ReturnsNull()
        {
            var dto = new UpdateChapterDto
            {
                ChapterName = "Updated Chapter",
                Description = "Updated description"
            };

            _chapterRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Chapter)null);

            var result = await _service.UpdateAsync(99, dto);

            result.Should().BeNull();
        }

        // =============================
        // 7. UpdateAsync - Update chapter name only
        // =============================
        [Test]
        public async Task UpdateAsync_WhenUpdatingNameOnly_ShouldUpdateSuccessfully()
        {
            var dto = new UpdateChapterDto
            {
                ChapterName = "Updated Chapter",
                Description = null // Not updating description
            };
            var chapter = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "Old Chapter", 
                Description = "Old description"
            };
            var updatedChapter = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "Updated Chapter", 
                Description = "Old description",
                ChapterLeader = new User { UserId = 1, FullName = "Leader Name" }
            };
            var chapterDto = new ChapterDto 
            { 
                ChapterId = 1, 
                ChapterName = "Updated Chapter", 
                Description = "Old description",
                ChapterLeaderName = "Leader Name"
            };

            _chapterRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chapter);
            _chapterRepo.Setup(r => r.Update(It.IsAny<Chapter>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _chapterRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, object>>[]>()))
                .ReturnsAsync(updatedChapter);
            _mapperMock.Setup(m => m.Map<ChapterDto>(updatedChapter)).Returns(chapterDto);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().NotBeNull();
            result.ChapterName.Should().Be("Updated Chapter");
            result.Description.Should().Be("Old description"); // Should remain unchanged
            chapter.ChapterName.Should().Be("Updated Chapter");
            chapter.Description.Should().Be("Old description"); // Should remain unchanged

            _chapterRepo.Verify(r => r.Update(chapter), Times.Once);
        }

        // =============================
        // 8. UpdateAsync - Update description only
        // =============================
        [Test]
        public async Task UpdateAsync_WhenUpdatingDescriptionOnly_ShouldUpdateSuccessfully()
        {
            var dto = new UpdateChapterDto
            {
                ChapterName = "", // Empty string, should not update
                Description = "Updated description"
            };
            var chapter = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "Old Chapter", 
                Description = "Old description"
            };
            var updatedChapter = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "Old Chapter", 
                Description = "Updated description",
                ChapterLeader = new User { UserId = 1, FullName = "Leader Name" }
            };
            var chapterDto = new ChapterDto 
            { 
                ChapterId = 1, 
                ChapterName = "Old Chapter", 
                Description = "Updated description",
                ChapterLeaderName = "Leader Name"
            };

            _chapterRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chapter);
            _chapterRepo.Setup(r => r.Update(It.IsAny<Chapter>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _chapterRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, object>>[]>()))
                .ReturnsAsync(updatedChapter);
            _mapperMock.Setup(m => m.Map<ChapterDto>(updatedChapter)).Returns(chapterDto);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().NotBeNull();
            result.ChapterName.Should().Be("Old Chapter"); // Should remain unchanged
            result.Description.Should().Be("Updated description");
            chapter.ChapterName.Should().Be("Old Chapter"); // Should remain unchanged
            chapter.Description.Should().Be("Updated description");

            _chapterRepo.Verify(r => r.Update(chapter), Times.Once);
        }

        // =============================
        // 9. UpdateAsync - Update both name and description
        // =============================
        [Test]
        public async Task UpdateAsync_WhenUpdatingBoth_ShouldUpdateSuccessfully()
        {
            var dto = new UpdateChapterDto
            {
                ChapterName = "Updated Chapter",
                Description = "Updated description"
            };
            var chapter = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "Old Chapter", 
                Description = "Old description"
            };
            var updatedChapter = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "Updated Chapter", 
                Description = "Updated description",
                ChapterLeader = new User { UserId = 1, FullName = "Leader Name" }
            };
            var chapterDto = new ChapterDto 
            { 
                ChapterId = 1, 
                ChapterName = "Updated Chapter", 
                Description = "Updated description",
                ChapterLeaderName = "Leader Name"
            };

            _chapterRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chapter);
            _chapterRepo.Setup(r => r.Update(It.IsAny<Chapter>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _chapterRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Chapter, object>>[]>()))
                .ReturnsAsync(updatedChapter);
            _mapperMock.Setup(m => m.Map<ChapterDto>(updatedChapter)).Returns(chapterDto);

            var result = await _service.UpdateAsync(1, dto);

            result.Should().NotBeNull();
            result.ChapterName.Should().Be("Updated Chapter");
            result.Description.Should().Be("Updated description");
            chapter.ChapterName.Should().Be("Updated Chapter");
            chapter.Description.Should().Be("Updated description");

            _chapterRepo.Verify(r => r.Update(chapter), Times.Once);
        }

        // =============================
        // 10. DeleteAsync - Chapter not found
        // =============================
        [Test]
        public async Task DeleteAsync_WhenChapterNotFound_ReturnsFalse()
        {
            _chapterRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Chapter)null);

            var result = await _service.DeleteAsync(99, 1);

            result.Should().BeFalse();
        }

        // =============================
        // 11. DeleteAsync - User not chapter leader
        // =============================
        [Test]
        public void DeleteAsync_WhenUserNotLeader_Throws()
        {
            var chapter = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "Test Chapter",
                ChapterLeaderId = 2 // Different leader
            };

            _chapterRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chapter);

            Func<Task> act = async () => await _service.DeleteAsync(1, 1); // User 1 trying to delete, but leader is 2

            act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not the leader of this chapter.");
        }

        // =============================
        // 12. DeleteAsync - Successful deletion
        // =============================
        [Test]
        public async Task DeleteAsync_WhenUserIsLeader_ShouldDeleteSuccessfully()
        {
            var chapter = new Chapter 
            { 
                ChapterId = 1, 
                ChapterName = "Test Chapter",
                ChapterLeaderId = 1 // Same as user
            };

            _chapterRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chapter);
            _chapterRepo.Setup(r => r.Remove(It.IsAny<Chapter>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            var result = await _service.DeleteAsync(1, 1);

            result.Should().BeTrue();
            _chapterRepo.Verify(r => r.Remove(chapter), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }
    }
}