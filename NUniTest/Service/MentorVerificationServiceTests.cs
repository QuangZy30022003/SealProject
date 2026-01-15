using AutoMapper;
using Common.DTOs.MentorVerificationDto;
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
    public class MentorVerificationServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IFileUploadService> _fileUploadServiceMock;
        private MentorVerificationService _service;

        private Mock<IRepository<MentorVerification>> _mentorVerificationRepo;
        private Mock<IRepository<Chapter>> _chapterRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _fileUploadServiceMock = new Mock<IFileUploadService>();

            _mentorVerificationRepo = new Mock<IRepository<MentorVerification>>();
            _chapterRepo = new Mock<IRepository<Chapter>>();

            _uowMock.Setup(u => u.MentorVerifications).Returns(_mentorVerificationRepo.Object);
            _uowMock.Setup(u => u.Chapters).Returns(_chapterRepo.Object);

            _service = new MentorVerificationService(_uowMock.Object, _mapperMock.Object, _fileUploadServiceMock.Object);
        }

        // =============================
        // 1. CreateAsync - Duplicate pending request
        // =============================
        [Test]
        public void CreateAsync_WhenDuplicatePendingRequest_Throws()
        {
            var dto = new MentorVerificationCreateDto
            {
                HackathonId = 1,
                ChapterId = 1,
                Position = "doctor"
            };
            var cvFile = new Mock<IFormFile>().Object;

            _mentorVerificationRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>()))
                .ReturnsAsync(true);

            Func<Task> act = async () => await _service.CreateAsync(dto, cvFile, 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You have already sent a mentor request!");
        }

        // =============================
        // 2. CreateAsync - Successful creation with CV file
        // =============================
        [Test]
        public async Task CreateAsync_WithCvFile_ShouldCreateSuccessfully()
        {
            var dto = new MentorVerificationCreateDto
            {
                HackathonId = 1,
                ChapterId = 1,
                Position = "doctor"
            };
            var cvFile = new Mock<IFormFile>().Object;
            var chapter = new Chapter { ChapterId = 1, ChapterName = "Test Chapter" };
            var entity = new MentorVerification 
            { 
                Id = 1, 
                HackathonId = 1, 
                ChapterId = 1, 
                UserId = 1,
                Status = "Pending",
                Chapter = chapter
            };
            var responseDto = new MentorVerificationResponseDto 
            { 
                Id = 1, 
                HackathonId = 1, 
                ChapterId = 1,
                ChapterName = "Test Chapter",
                Status = "Pending"
            };

            _mentorVerificationRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>()))
                .ReturnsAsync(false);
            _mapperMock.Setup(m => m.Map<MentorVerification>(dto)).Returns(entity);
            _chapterRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chapter);
            _fileUploadServiceMock.Setup(f => f.UploadStudnetAsync(cvFile)).ReturnsAsync("cv-url.pdf");
            _mentorVerificationRepo.Setup(r => r.AddAsync(It.IsAny<MentorVerification>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<MentorVerificationResponseDto>(entity)).Returns(responseDto);

            var result = await _service.CreateAsync(dto, cvFile, 1);

            result.Should().NotBeNull();
            result.Status.Should().Be("Pending");
            result.ChapterName.Should().Be("Test Chapter");
            entity.Cv.Should().Be("cv-url.pdf");

            _mentorVerificationRepo.Verify(r => r.AddAsync(It.IsAny<MentorVerification>()), Times.Once);
        }

        // =============================
        // 3. CreateAsync - Without CV file
        // =============================
        [Test]
        public async Task CreateAsync_WithoutCvFile_ShouldCreateSuccessfully()
        {
            var dto = new MentorVerificationCreateDto
            {
                HackathonId = 1,
                ChapterId = 1,
                Position = "doctor"
            };
            var chapter = new Chapter { ChapterId = 1, ChapterName = "Test Chapter" };
            var entity = new MentorVerification 
            { 
                Id = 1, 
                HackathonId = 1, 
                ChapterId = 1, 
                UserId = 1,
                Status = "Pending",
                Chapter = chapter
            };
            var responseDto = new MentorVerificationResponseDto 
            { 
                Id = 1, 
                HackathonId = 1, 
                ChapterId = 1,
                ChapterName = "Test Chapter",
                Status = "Pending"
            };

            _mentorVerificationRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>()))
                .ReturnsAsync(false);
            _mapperMock.Setup(m => m.Map<MentorVerification>(dto)).Returns(entity);
            _chapterRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chapter);
            _mentorVerificationRepo.Setup(r => r.AddAsync(It.IsAny<MentorVerification>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<MentorVerificationResponseDto>(entity)).Returns(responseDto);

            var result = await _service.CreateAsync(dto, null, 1);

            result.Should().NotBeNull();
            result.Status.Should().Be("Pending");
            entity.Cv.Should().BeNull();

            _fileUploadServiceMock.Verify(f => f.UploadStudnetAsync(It.IsAny<IFormFile>()), Times.Never);
        }

        // =============================
        // 4. CreateAsync - Chapter not found
        // =============================
        [Test]
        public async Task CreateAsync_WhenChapterNotFound_ShouldCreateWithoutChapter()
        {
            var dto = new MentorVerificationCreateDto
            {
                HackathonId = 1,
                ChapterId = 99,
                Position = "doctor"
            };
            var entity = new MentorVerification 
            { 
                Id = 1, 
                HackathonId = 1, 
                UserId = 1,
                Status = "Pending"
            };
            var responseDto = new MentorVerificationResponseDto 
            { 
                Id = 1, 
                HackathonId = 1, 
                Status = "Pending"
            };

            _mentorVerificationRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>()))
                .ReturnsAsync(false);
            _mapperMock.Setup(m => m.Map<MentorVerification>(dto)).Returns(entity);
            _chapterRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Chapter)null);
            _mentorVerificationRepo.Setup(r => r.AddAsync(It.IsAny<MentorVerification>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<MentorVerificationResponseDto>(entity)).Returns(responseDto);

            var result = await _service.CreateAsync(dto, null, 1);

            result.Should().NotBeNull();
            entity.ChapterId.Should().BeNull();
        }

        // =============================
        // 5. GetAllAsync - Return all mentor verifications
        // =============================
        [Test]
        public async Task GetAllAsync_ShouldReturnAllMentorVerifications()
        {
            var verifications = new List<MentorVerification>
            {
                new MentorVerification { Id = 1, Status = "Pending" },
                new MentorVerification { Id = 2, Status = "Approved" }
            };
            var verificationDtos = new List<MentorVerificationResponseDto>
            {
                new MentorVerificationResponseDto { Id = 1, Status = "Pending" },
                new MentorVerificationResponseDto { Id = 2, Status = "Approved" }
            };

            _mentorVerificationRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<MentorVerification, bool>>>(),
        It.IsAny<Func<IQueryable<MentorVerification>, IOrderedQueryable<MentorVerification>>>(),
        It.IsAny<string>()
    ))
    .ReturnsAsync(verifications);

            _mapperMock.Setup(m => m.Map<List<MentorVerificationResponseDto>>(verifications)).Returns(verificationDtos);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().Status.Should().Be("Pending");
            result.Last().Status.Should().Be("Approved");
        }

        // =============================
        // 6. ApproveAsync - Verification not found
        // =============================
        [Test]
        public async Task ApproveAsync_WhenVerificationNotFound_ReturnsNull()
        {
            _mentorVerificationRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, object>>[]>()))
                .ReturnsAsync((MentorVerification)null);

            var result = await _service.ApproveAsync(99, 1);

            result.Should().BeNull();
        }

        // =============================
        // 7. ApproveAsync - Unauthorized chapter leader
        // =============================
        [Test]
        public void ApproveAsync_WhenUnauthorizedChapterLeader_Throws()
        {
            var verification = new MentorVerification 
            { 
                Id = 1, 
                ChapterId = 1,
                Chapter = new Chapter { ChapterId = 1, ChapterLeaderId = 2 }, // Different leader
                User = new User { UserId = 1, RoleId = 3 }
            };

            _mentorVerificationRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, object>>[]>()))
                .ReturnsAsync(verification);

            Func<Task> act = async () => await _service.ApproveAsync(1, 1); // ChapterLeaderId = 1, but actual is 2

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not authorized to approve this mentor.");
        }

        // =============================
        // 8. ApproveAsync - Successful approval
        // =============================
        [Test]
        public async Task ApproveAsync_WhenAuthorized_ShouldApproveSuccessfully()
        {
            var verification = new MentorVerification 
            { 
                Id = 1, 
                ChapterId = 1,
                Status = "Pending",
                Chapter = new Chapter { ChapterId = 1, ChapterLeaderId = 1 },
                User = new User { UserId = 1, RoleId = 3 }
            };
            var responseDto = new MentorVerificationResponseDto 
            { 
                Id = 1, 
                Status = "Approved"
            };

            _mentorVerificationRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, object>>[]>()))
                .ReturnsAsync(verification);
            _mentorVerificationRepo.Setup(r => r.Update(It.IsAny<MentorVerification>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<MentorVerificationResponseDto>(verification)).Returns(responseDto);

            var result = await _service.ApproveAsync(1, 1);

            result.Should().NotBeNull();
            result.Status.Should().Be("Approved");
            verification.Status.Should().Be("Approved");
            verification.User.RoleId.Should().Be(5); // Mentor role
            verification.RejectReason.Should().BeNull();

            _mentorVerificationRepo.Verify(r => r.Update(verification), Times.Once);
        }

        // =============================
        // 9. RejectAsync - Verification not found
        // =============================
        [Test]
        public async Task RejectAsync_WhenVerificationNotFound_ReturnsNull()
        {
            _mentorVerificationRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, object>>[]>()))
                .ReturnsAsync((MentorVerification)null);

            var result = await _service.RejectAsync(99, "Invalid experience", 1);

            result.Should().BeNull();
        }

        // =============================
        // 10. RejectAsync - Unauthorized chapter leader
        // =============================
        [Test]
        public void RejectAsync_WhenUnauthorizedChapterLeader_Throws()
        {
            var verification = new MentorVerification 
            { 
                Id = 1, 
                ChapterId = 1,
                Chapter = new Chapter { ChapterId = 1, ChapterLeaderId = 2 },
                User = new User { UserId = 1, RoleId = 3 }
            };

            _mentorVerificationRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, object>>[]>()))
                .ReturnsAsync(verification);

            Func<Task> act = async () => await _service.RejectAsync(1, "Invalid experience", 1);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not authorized to reject this mentor.");
        }

        // =============================
        // 11. RejectAsync - Successful rejection
        // =============================
        [Test]
        public async Task RejectAsync_WhenAuthorized_ShouldRejectSuccessfully()
        {
            var verification = new MentorVerification 
            { 
                Id = 1, 
                ChapterId = 1,
                Status = "Pending",
                Chapter = new Chapter { ChapterId = 1, ChapterLeaderId = 1 },
                User = new User { UserId = 1, RoleId = 3 }
            };
            var responseDto = new MentorVerificationResponseDto 
            { 
                Id = 1, 
                Status = "Reject",
                RejectReason = "Insufficient experience"
            };

            _mentorVerificationRepo.Setup(r => r.GetByIdIncludingAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, object>>[]>()))
                .ReturnsAsync(verification);
            _mentorVerificationRepo.Setup(r => r.Update(It.IsAny<MentorVerification>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<MentorVerificationResponseDto>(verification)).Returns(responseDto);

            var result = await _service.RejectAsync(1, "Insufficient experience", 1);

            result.Should().NotBeNull();
            result.Status.Should().Be("Reject");
            result.RejectReason.Should().Be("Insufficient experience");
            verification.Status.Should().Be("Reject");
            verification.RejectReason.Should().Be("Insufficient experience");

            _mentorVerificationRepo.Verify(r => r.Update(verification), Times.Once);
        }

        // =============================
        // 12. GetApprovedMentorsByHackathonAsync - Return approved mentors
        // =============================
        [Test]
        public async Task GetApprovedMentorsByHackathonAsync_ShouldReturnApprovedMentors()
        {
            var approvedMentors = new List<MentorVerification>
            {
                new MentorVerification { Id = 1, HackathonId = 1, Status = "Approved" },
                new MentorVerification { Id = 2, HackathonId = 1, Status = "Approved" }
            };
            var mentorDtos = new List<MentorVerificationResponseDto>
            {
                new MentorVerificationResponseDto { Id = 1, HackathonId = 1, Status = "Approved" },
                new MentorVerificationResponseDto { Id = 2, HackathonId = 1, Status = "Approved" }
            };

            _mentorVerificationRepo.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<MentorVerification, bool>>>(),
                It.IsAny<Func<IQueryable<MentorVerification>, IOrderedQueryable<MentorVerification>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(approvedMentors);
            _mapperMock.Setup(m => m.Map<List<MentorVerificationResponseDto>>(approvedMentors)).Returns(mentorDtos);

            var result = await _service.GetApprovedMentorsByHackathonAsync(1);

            result.Should().HaveCount(2);
            result.All(m => m.Status == "Approved").Should().BeTrue();
            result.All(m => m.HackathonId == 1).Should().BeTrue();
        }
    }
}