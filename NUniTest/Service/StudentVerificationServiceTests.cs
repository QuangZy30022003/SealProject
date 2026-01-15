using AutoMapper;
using Common.DTOs.NotificationDto;
using Common.DTOs.StudentVerification;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
    public class StudentVerificationServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IConfiguration> _configMock;
        private Mock<IFileUploadService> _fileUploadServiceMock;
        private Mock<INotificationService> _notificationServiceMock;
        private StudentVerificationService _service;

        private Mock<IRepository<StudentVerification>> _studentVerificationRepo;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _configMock = new Mock<IConfiguration>();
            _fileUploadServiceMock = new Mock<IFileUploadService>();
            _notificationServiceMock = new Mock<INotificationService>();

            _studentVerificationRepo = new Mock<IRepository<StudentVerification>>();

            _uowMock.Setup(u => u.StudentVerifications).Returns(_studentVerificationRepo.Object);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);


            _service = new StudentVerificationService(
                _uowMock.Object, 
                _mapperMock.Object, 
                _configMock.Object, 
                _fileUploadServiceMock.Object, 
                _notificationServiceMock.Object);
        }

        [Test]
        public void SubmitAsync_WhenUserAlreadySubmitted_Throws()
        {
            var dto = new StudentVerificationDto
            {
                StudentCode = "SV001",
                FullName = "Test Student",
                UniversityName = "Test University"
            };
            var existingVerification = new StudentVerification { UserId = 1, Status = "Pending" };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync(existingVerification);

            Func<Task> act = async () => await _service.SubmitAsync(1, "test@email.com", dto);

            act.Should().ThrowAsync<Exception>()
                .WithMessage("Bạn đã gửi yêu cầu xác thực trước đó.");
        }

        [Test]
        public async Task SubmitAsync_WithoutImages_ShouldSubmitSuccessfully()
        {
            var dto = new StudentVerificationDto
            {
                StudentCode = "SV001",
                FullName = "Test Student",
                UniversityName = "Test University",
                FrontCardImage = null,
                BackCardImage = null
            };
            var verification = new StudentVerification
            {
                UserId = 1,
                StudentEmail = "test@email.com",
                StudentCode = "SV001",
                FullName = "Test Student",
                UniversityName = "Test University",
                Status = "Pending"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync((StudentVerification)null);
            _mapperMock.Setup(m => m.Map<StudentVerification>(dto)).Returns(verification);
            _studentVerificationRepo.Setup(r => r.AddAsync(It.IsAny<StudentVerification>())).Returns(Task.CompletedTask);

            await _service.SubmitAsync(1, "test@email.com", dto);

            verification.UserId.Should().Be(1);
            verification.StudentEmail.Should().Be("test@email.com");
            verification.Status.Should().Be("Pending");
            verification.FrontCardImage.Should().BeNull();
            verification.BackCardImage.Should().BeNull();

            _studentVerificationRepo.Verify(r => r.AddAsync(verification), Times.Once);
        }

        [Test]
        public async Task SubmitAsync_WithImages_ShouldUploadAndSubmitSuccessfully()
        {
            var frontImageMock = new Mock<IFormFile>();
            var backImageMock = new Mock<IFormFile>();
            var dto = new StudentVerificationDto
            {
                StudentCode = "SV001",
                FullName = "Test Student",
                UniversityName = "Test University",
                FrontCardImage = frontImageMock.Object,
                BackCardImage = backImageMock.Object
            };
            var verification = new StudentVerification
            {
                UserId = 1,
                StudentEmail = "test@email.com",
                StudentCode = "SV001",
                FullName = "Test Student",
                UniversityName = "Test University",
                Status = "Pending"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync((StudentVerification)null);
            _mapperMock.Setup(m => m.Map<StudentVerification>(dto)).Returns(verification);
            _fileUploadServiceMock.Setup(f => f.UploadStudnetAsync(frontImageMock.Object)).ReturnsAsync("front-image-url");
            _fileUploadServiceMock.Setup(f => f.UploadStudnetAsync(backImageMock.Object)).ReturnsAsync("back-image-url");
            _studentVerificationRepo.Setup(r => r.AddAsync(It.IsAny<StudentVerification>())).Returns(Task.CompletedTask);

            await _service.SubmitAsync(1, "test@email.com", dto);

            verification.FrontCardImage.Should().Be("front-image-url");
            verification.BackCardImage.Should().Be("back-image-url");

            _fileUploadServiceMock.Verify(f => f.UploadStudnetAsync(frontImageMock.Object), Times.Once);
            _fileUploadServiceMock.Verify(f => f.UploadStudnetAsync(backImageMock.Object), Times.Once);
            _studentVerificationRepo.Verify(r => r.AddAsync(verification), Times.Once);
        }

        [Test]
        public async Task SubmitAsync_WithOnlyFrontImage_ShouldUploadFrontImageOnly()
        {
            var frontImageMock = new Mock<IFormFile>();
            var dto = new StudentVerificationDto
            {
                StudentCode = "SV001",
                FullName = "Test Student",
                UniversityName = "Test University",
                FrontCardImage = frontImageMock.Object,
                BackCardImage = null
            };
            var verification = new StudentVerification
            {
                UserId = 1,
                StudentEmail = "test@email.com",
                Status = "Pending"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync((StudentVerification)null);
            _mapperMock.Setup(m => m.Map<StudentVerification>(dto)).Returns(verification);
            _fileUploadServiceMock.Setup(f => f.UploadStudnetAsync(frontImageMock.Object)).ReturnsAsync("front-image-url");
            _studentVerificationRepo.Setup(r => r.AddAsync(It.IsAny<StudentVerification>())).Returns(Task.CompletedTask);

            await _service.SubmitAsync(1, "test@email.com", dto);

            verification.FrontCardImage.Should().Be("front-image-url");
            verification.BackCardImage.Should().BeNull();

            _fileUploadServiceMock.Verify(f => f.UploadStudnetAsync(frontImageMock.Object), Times.Once);
            _fileUploadServiceMock.Verify(f => f.UploadStudnetAsync(It.IsAny<IFormFile>()), Times.Once);
        }

        [Test]
        public async Task ApproveVerificationByUserIdAsync_WhenVerificationNotFound_ReturnsFalse()
        {
            var userId = 99;

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync((StudentVerification)null);

            var result = await _service.ApproveVerificationByUserIdAsync(userId);

            result.Should().BeFalse();
        }

        [Test]
        public void ApproveVerificationByUserIdAsync_WhenStatusNotPendingOrRejected_Throws()
        {
            var userId = 1;
            var verification = new StudentVerification 
            { 
                VerificationId = 1, 
                UserId = userId, 
                Status = "Approved"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync(verification);

            Func<Task> act = async () => await _service.ApproveVerificationByUserIdAsync(userId);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Chỉ có thể duyệt các yêu cầu đang ở trạng thái Pending hoặc Rejected.");
        }

        [Test]
        public async Task ApproveVerificationByUserIdAsync_FromPendingStatus_ShouldApproveSuccessfully()
        {
            var userId = 1;
            var verification = new StudentVerification 
            { 
                VerificationId = 1, 
                UserId = userId, 
                Status = "Pending"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync(verification);
            _studentVerificationRepo.Setup(r => r.Update(It.IsAny<StudentVerification>()));
            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto());

            var result = await _service.ApproveVerificationByUserIdAsync(userId);

            result.Should().BeTrue();
            verification.Status.Should().Be("Approved");
            verification.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));

            _studentVerificationRepo.Verify(r => r.Update(verification), Times.Once);
            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.Is<CreateNotificationDto>(
                dto => dto.UserId == userId && dto.Message.Contains("approved"))), Times.Once);
        }

        [Test]
        public async Task ApproveVerificationByUserIdAsync_FromRejectedStatus_ShouldApproveSuccessfully()
        {
            var userId = 1;
            var verification = new StudentVerification 
            { 
                VerificationId = 1, 
                UserId = userId, 
                Status = "Rejected"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync(verification);
            _studentVerificationRepo.Setup(r => r.Update(It.IsAny<StudentVerification>()));
            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto());

            var result = await _service.ApproveVerificationByUserIdAsync(userId);

            result.Should().BeTrue();
            verification.Status.Should().Be("Approved");

            _studentVerificationRepo.Verify(r => r.Update(verification), Times.Once);
            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()), Times.Once);
        }

        [Test]
        public async Task RejectVerificationByUserIdAsync_WhenVerificationNotFound_ReturnsFalse()
        {
            var userId = 99;

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync((StudentVerification)null);

            var result = await _service.RejectVerificationByUserIdAsync(userId);

            result.Should().BeFalse();
        }

        [Test]
        public void RejectVerificationByUserIdAsync_WhenStatusApproved_Throws()
        {
            var userId = 1;
            var verification = new StudentVerification 
            { 
                VerificationId = 1, 
                UserId = userId, 
                Status = "Approved"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync(verification);

            Func<Task> act = async () => await _service.RejectVerificationByUserIdAsync(userId);

            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Không thể từ chối yêu cầu đã được duyệt.");
        }

        [Test]
        public async Task RejectVerificationByUserIdAsync_FromPendingStatus_ShouldRejectSuccessfully()
        {
            var userId = 1;
            var verification = new StudentVerification 
            { 
                VerificationId = 1, 
                UserId = userId, 
                Status = "Pending"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync(verification);
            _studentVerificationRepo.Setup(r => r.Update(It.IsAny<StudentVerification>()));
            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto());

            var result = await _service.RejectVerificationByUserIdAsync(userId);

            result.Should().BeTrue();
            verification.Status.Should().Be("Rejected");
            verification.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));

            _studentVerificationRepo.Verify(r => r.Update(verification), Times.Once);
            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.Is<CreateNotificationDto>(
                dto => dto.UserId == userId && dto.Message.Contains("rejected"))), Times.Once);
        }

        [Test]
        public async Task RejectVerificationByUserIdAsync_FromRejectedStatus_ShouldStillWork()
        {
            var userId = 1;
            var verification = new StudentVerification 
            { 
                VerificationId = 1, 
                UserId = userId, 
                Status = "Rejected"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync(verification);
            _studentVerificationRepo.Setup(r => r.Update(It.IsAny<StudentVerification>()));
            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto());

            var result = await _service.RejectVerificationByUserIdAsync(userId);

            result.Should().BeTrue();
            verification.Status.Should().Be("Rejected");

            _studentVerificationRepo.Verify(r => r.Update(verification), Times.Once);
        }

        [Test]
        public async Task GetPendingOrRejectedVerificationsAsync_ShouldReturnPendingAndRejectedOnly()
        {
            var verifications = new List<StudentVerification>
            {
                new StudentVerification { VerificationId = 1, UserId = 1, Status = "Pending" },
                new StudentVerification { VerificationId = 2, UserId = 2, Status = "Rejected" },
                new StudentVerification { VerificationId = 3, UserId = 3, Status = "Approved" }
            };
            var filteredVerifications = verifications.Where(v => v.Status == "Pending" || v.Status == "Rejected").ToList();
            var adminDtos = new List<StudentVerificationAdminDto>
            {
                new StudentVerificationAdminDto { UserId = 1, Status = "Pending" },
                new StudentVerificationAdminDto { UserId = 2, Status = "Rejected" }
            };

            _studentVerificationRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<StudentVerification, bool>>>(),
        It.IsAny<Func<IQueryable<StudentVerification>, IOrderedQueryable<StudentVerification>>>(),
        It.IsAny<string>())).ReturnsAsync(filteredVerifications);
            _mapperMock.Setup(m => m.Map<List<StudentVerificationAdminDto>>(filteredVerifications)).Returns(adminDtos);

            var result = await _service.GetPendingOrRejectedVerificationsAsync();

            result.Should().HaveCount(2);
            result.Should().Contain(v => v.Status == "Pending");
            result.Should().Contain(v => v.Status == "Rejected");
            result.Should().NotContain(v => v.Status == "Approved");
        }

        [Test]
        public async Task GetPendingOrRejectedVerificationsAsync_WhenNoVerifications_ShouldReturnEmptyList()
        {
            var emptyVerifications = new List<StudentVerification>();
            var emptyAdminDtos = new List<StudentVerificationAdminDto>();

            _studentVerificationRepo.Setup(r => r.GetAllAsync(
        It.IsAny<Expression<Func<StudentVerification, bool>>>(),
        It.IsAny<Func<IQueryable<StudentVerification>, IOrderedQueryable<StudentVerification>>>(),
        It.IsAny<string>())).ReturnsAsync(emptyVerifications);
            _mapperMock.Setup(m => m.Map<List<StudentVerificationAdminDto>>(emptyVerifications)).Returns(emptyAdminDtos);

            var result = await _service.GetPendingOrRejectedVerificationsAsync();

            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetMyVerificationAsync_WhenExists_ShouldReturnVerification()
        {
            var userId = 1;
            var verification = new StudentVerification 
            { 
                VerificationId = 1, 
                UserId = userId, 
                Status = "Pending",
                StudentCode = "SV001"
            };
            var dto = new StudentVerificationAdminDto 
            { 
                UserId = userId, 
                Status = "Pending"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync(verification);
            _mapperMock.Setup(m => m.Map<StudentVerificationAdminDto>(verification)).Returns(dto);

            var result = await _service.GetMyVerificationAsync(userId);

            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.Status.Should().Be("Pending");
        }

        [Test]
        public async Task GetMyVerificationAsync_WhenNotExists_ShouldReturnNull()
        {
            var userId = 999;

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync((StudentVerification)null);

            var result = await _service.GetMyVerificationAsync(userId);

            result.Should().BeNull();
        }

        [Test]
        public async Task SubmitAsync_ShouldSetAllPropertiesCorrectly()
        {
            var dto = new StudentVerificationDto
            {
                StudentCode = "SV123456",
                FullName = "Nguyen Van A",
                UniversityName = "FPT University",
                FrontCardImage = null,
                BackCardImage = null
            };
            var verification = new StudentVerification();

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync((StudentVerification)null);
            _mapperMock.Setup(m => m.Map<StudentVerification>(dto)).Returns(verification);
            _studentVerificationRepo.Setup(r => r.AddAsync(It.IsAny<StudentVerification>())).Returns(Task.CompletedTask);

            await _service.SubmitAsync(123, "student@fpt.edu.vn", dto);

            verification.UserId.Should().Be(123);
            verification.StudentEmail.Should().Be("student@fpt.edu.vn");
            verification.Status.Should().Be("Pending");
            verification.FrontCardImage.Should().BeNull();
            verification.BackCardImage.Should().BeNull();

            _mapperMock.Verify(m => m.Map<StudentVerification>(dto), Times.Once);
            _studentVerificationRepo.Verify(r => r.AddAsync(verification), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }

        [Test]
        public async Task ApproveVerificationByUserIdAsync_ShouldSendCorrectNotificationMessage()
        {
            var userId = 5;
            var verification = new StudentVerification 
            { 
                VerificationId = 1, 
                UserId = userId, 
                Status = "Pending"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync(verification);
            _studentVerificationRepo.Setup(r => r.Update(It.IsAny<StudentVerification>()));
            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto());

            await _service.ApproveVerificationByUserIdAsync(userId);

            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.Is<CreateNotificationDto>(
                dto => dto.UserId == userId && 
                       dto.Message == "Your student verification has been approved! You can now create teams.")), 
                Times.Once);
        }

        [Test]
        public async Task RejectVerificationByUserIdAsync_ShouldSendCorrectNotificationMessage()
        {
            var userId = 7;
            var verification = new StudentVerification 
            { 
                VerificationId = 1, 
                UserId = userId, 
                Status = "Pending"
            };

            _studentVerificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StudentVerification, bool>>>()))
                .ReturnsAsync(verification);
            _studentVerificationRepo.Setup(r => r.Update(It.IsAny<StudentVerification>()));
            _notificationServiceMock.Setup(n => n.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
                .ReturnsAsync(new NotificationDto());

            await _service.RejectVerificationByUserIdAsync(userId);

            _notificationServiceMock.Verify(n => n.CreateNotificationAsync(It.Is<CreateNotificationDto>(
                dto => dto.UserId == userId && 
                       dto.Message == "Your student verification has been rejected. Please resubmit with correct documents.")), 
                Times.Once);
        }
    }
}
