using AutoMapper;
using Common.DTOs.NotificationDto;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Repositories.Interface;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Hubs;
using Service.Servicefolder;
using System.Linq.Expressions;

namespace NUniTest.Service
{
    [TestFixture]
    public class NotificationServiceTests
    {
        private Mock<IUOW> _uowMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILogger<NotificationService>> _loggerMock;
        private Mock<IHubContext<ChatHub>> _hubContextMock;
        private NotificationService _service;

        private Mock<IRepository<Notification>> _notificationRepo;
        private Mock<IHubClients> _clientsMock;
        private Mock<IClientProxy> _clientProxyMock;

        [SetUp]
        public void Setup()
        {
            _uowMock = new Mock<IUOW>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<NotificationService>>();
            _hubContextMock = new Mock<IHubContext<ChatHub>>();

            _notificationRepo = new Mock<IRepository<Notification>>();
            _clientsMock = new Mock<IHubClients>();
            _clientProxyMock = new Mock<IClientProxy>();

            _uowMock.Setup(u => u.Notifications).Returns(_notificationRepo.Object);

            _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
            _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

            _service = new NotificationService(_uowMock.Object, _mapperMock.Object, _loggerMock.Object, _hubContextMock.Object);
        }

        // =============================
        // 1. CreateNotificationAsync - Tạo notification thành công
        // =============================
        [Test]
        public async Task CreateNotificationAsync_WhenValid_ShouldCreateAndBroadcast()
        {
            var dto = new CreateNotificationDto { UserId = 1, Message = "Test notification" };
            var notification = new Notification { NotificationId = 1, UserId = 1, Message = "Test notification", IsRead = false };
            var notificationDto = new NotificationDto { NotificationId = 1, UserId = 1, Message = "Test notification", IsRead = false };

            _notificationRepo.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>()))
                      .Returns(notificationDto);

            _clientProxyMock.Setup(c => c.SendCoreAsync(
                "ReceiveNotification", 
                It.IsAny<object[]>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _service.CreateNotificationAsync(dto);

            result.Should().NotBeNull();
            result.UserId.Should().Be(1);
            result.Message.Should().Be("Test notification");

            _notificationRepo.Verify(r => r.AddAsync(It.Is<Notification>(n => 
                n.UserId == 1 && 
                n.Message == "Test notification" && 
                n.IsRead == false)), Times.Once);

            _clientsMock.Verify(c => c.Group("User_1"), Times.Once);
        }

        // =============================
        // 2. CreateNotificationsAsync - Tạo nhiều notifications thành công
        // =============================
        [Test]
        public async Task CreateNotificationsAsync_WhenValid_ShouldCreateMultipleAndBroadcast()
        {
            var userIds = new List<int> { 1, 2, 3 };
            var message = "Bulk notification";
            var notifications = userIds.Select(id => new Notification 
            { 
                NotificationId = id, 
                UserId = id, 
                Message = message, 
                IsRead = false 
            }).ToList();

            _notificationRepo.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            _mapperMock.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>()))
                      .Returns((Notification n) => new NotificationDto 
                      { 
                          NotificationId = n.NotificationId, 
                          UserId = n.UserId, 
                          Message = n.Message 
                      });

            _clientProxyMock.Setup(c => c.SendCoreAsync(
                "ReceiveNotification", 
                It.IsAny<object[]>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _service.CreateNotificationsAsync(userIds, message);

            _notificationRepo.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Exactly(3));
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());

            // Verify SignalR calls for each user
            _clientsMock.Verify(c => c.Group("User_1"), Times.Once);
            _clientsMock.Verify(c => c.Group("User_2"), Times.Once);
            _clientsMock.Verify(c => c.Group("User_3"), Times.Once);
        }

        // =============================
        // 3. GetUserNotificationsAsync - Lấy tất cả notifications
        // =============================
        [Test]
        public async Task GetUserNotificationsAsync_WhenUnreadOnlyFalse_ShouldReturnAllNotifications()
        {
            var notifications = new List<Notification>
            {
                new Notification { NotificationId = 1, UserId = 1, Message = "Notification 1", IsRead = false, SentAt = DateTime.UtcNow },
                new Notification { NotificationId = 2, UserId = 1, Message = "Notification 2", IsRead = true, SentAt = DateTime.UtcNow.AddMinutes(-1) }
            };
            var notificationDtos = new List<NotificationDto>
            {
                new NotificationDto { NotificationId = 1, UserId = 1, Message = "Notification 1", IsRead = false },
                new NotificationDto { NotificationId = 2, UserId = 1, Message = "Notification 2", IsRead = true }
            };

            _notificationRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Notification, bool>>>(), null, null))
                            .ReturnsAsync(notifications);

            _mapperMock.Setup(m => m.Map<IEnumerable<NotificationDto>>(It.IsAny<IEnumerable<Notification>>()))
                      .Returns(notificationDtos);

            var result = await _service.GetUserNotificationsAsync(1, false);

            result.Should().HaveCount(2);
            result.Should().Contain(n => n.NotificationId == 1);
            result.Should().Contain(n => n.NotificationId == 2);
        }

        // =============================
        // 4. GetUserNotificationsAsync - Chỉ lấy unread notifications
        // =============================
        [Test]
        public async Task GetUserNotificationsAsync_WhenUnreadOnlyTrue_ShouldReturnUnreadOnly()
        {
            var unreadNotifications = new List<Notification>
            {
                new Notification { NotificationId = 1, UserId = 1, Message = "Unread notification", IsRead = false, SentAt = DateTime.UtcNow }
            };
            var notificationDtos = new List<NotificationDto>
            {
                new NotificationDto { NotificationId = 1, UserId = 1, Message = "Unread notification", IsRead = false }
            };

            _notificationRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Notification, bool>>>(), null, null))
                            .ReturnsAsync(unreadNotifications);

            _mapperMock.Setup(m => m.Map<IEnumerable<NotificationDto>>(It.IsAny<IEnumerable<Notification>>()))
                      .Returns(notificationDtos);

            var result = await _service.GetUserNotificationsAsync(1, true);

            result.Should().HaveCount(1);
            result.First().IsRead.Should().BeFalse();
        }

        // =============================
        // 5. GetUnreadCountAsync - Đếm notifications chưa đọc
        // =============================
        [Test]
        public async Task GetUnreadCountAsync_ShouldReturnUnreadCount()
        {
            _notificationRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
                            .ReturnsAsync(5);

            var result = await _service.GetUnreadCountAsync(1);

            result.Should().Be(5);
        }
      

        // =============================
        // 8. DeleteNotificationAsync - Notification tồn tại và thuộc về user
        // =============================
        [Test]
        public async Task DeleteNotificationAsync_WhenNotificationExistsAndBelongsToUser_ShouldDelete()
        {
            var notification = new Notification { NotificationId = 1, UserId = 1 };

            _notificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
                            .ReturnsAsync(notification);

            _notificationRepo.Setup(r => r.Remove(It.IsAny<Notification>()));
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);

            await _service.DeleteNotificationAsync(1, 1);

            _notificationRepo.Verify(r => r.Remove(notification), Times.Once);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.AtLeastOnce());
        }

        // =============================
        // 9. DeleteNotificationAsync - Notification không tồn tại
        // =============================
        [Test]
        public async Task DeleteNotificationAsync_WhenNotificationNotExists_ShouldNotDelete()
        {
            _notificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
                            .ReturnsAsync((Notification)null);

            await _service.DeleteNotificationAsync(99, 1);

            _notificationRepo.Verify(r => r.Remove(It.IsAny<Notification>()), Times.Never);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Never());
        }

        // =============================
        // 10. DeleteNotificationAsync - Notification không thuộc về user
        // =============================
        [Test]
        public async Task DeleteNotificationAsync_WhenNotificationNotBelongsToUser_ShouldNotDelete()
        {
            _notificationRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
                            .ReturnsAsync((Notification)null); // Không tìm thấy với điều kiện userId

            await _service.DeleteNotificationAsync(1, 99); // User khác

            _notificationRepo.Verify(r => r.Remove(It.IsAny<Notification>()), Times.Never);
            _uowMock.Verify(u => u.SaveAsync(It.IsAny<int?>()), Times.Never());
        }

        // =============================
        // 11. CreateNotificationAsync - Verify logging
        // =============================
        [Test]
        public async Task CreateNotificationAsync_ShouldLogInformation()
        {
            var dto = new CreateNotificationDto { UserId = 1, Message = "Test" };
            var notification = new Notification { NotificationId = 1, UserId = 1 };
            var notificationDto = new NotificationDto { NotificationId = 1, UserId = 1 };

            _notificationRepo.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>())).Returns(notificationDto);

            _clientProxyMock.Setup(c => c.SendCoreAsync(
                It.IsAny<string>(), 
                It.IsAny<object[]>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _service.CreateNotificationAsync(dto);

            // Verify logging was called (using LogLevel.Information)
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Notification created for user")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // =============================
        // 12. CreateNotificationsAsync - Verify bulk logging
        // =============================
        [Test]
        public async Task CreateNotificationsAsync_ShouldLogBulkCreation()
        {
            var userIds = new List<int> { 1, 2 };
            var message = "Bulk test";

            _notificationRepo.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveAsync(It.IsAny<int?>())).ReturnsAsync(1);
            _mapperMock.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>()))
                      .Returns(new NotificationDto());

            _clientProxyMock.Setup(c => c.SendCoreAsync(
                It.IsAny<string>(), 
                It.IsAny<object[]>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _service.CreateNotificationsAsync(userIds, message);

            // Verify bulk logging
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Created 2 notifications")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}