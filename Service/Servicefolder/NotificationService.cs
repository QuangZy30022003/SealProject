using AutoMapper;
using Common.DTOs.NotificationDto;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Repositories.Models;
using Repositories.UnitOfWork;
using Service.Hubs;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Servicefolder
{
    public class NotificationService : INotificationService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public NotificationService(
            IUOW uow,
            IMapper mapper,
            ILogger<NotificationService> logger,
            IHubContext<ChatHub> hubContext)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto)
        {
            var notification = new Notification
            {
                UserId = dto.UserId,
                Message = dto.Message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _uow.Notifications.AddAsync(notification);
            await _uow.SaveAsync();

            _logger.LogInformation("Notification created for user {UserId}", dto.UserId);

            var result = _mapper.Map<NotificationDto>(notification);

            // Broadcast qua SignalR
            await _hubContext.Clients.Group($"User_{dto.UserId}")
                .SendAsync("ReceiveNotification", result);

            return result;
        }

        public async Task CreateNotificationsAsync(List<int> userIds, string message)
        {
            var notifications = userIds.Select(userId => new Notification
            {
                UserId = userId,
                Message = message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            }).ToList();

            foreach (var notification in notifications)
            {
                await _uow.Notifications.AddAsync(notification);
            }
            await _uow.SaveAsync();

            _logger.LogInformation("Created {Count} notifications", notifications.Count);

            // Broadcast qua SignalR cho từng user
            foreach (var notification in notifications)
            {
                var dto = _mapper.Map<NotificationDto>(notification);
                await _hubContext.Clients.Group($"User_{notification.UserId}")
                    .SendAsync("ReceiveNotification", dto);
            }
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
        {
            var notifications = unreadOnly
                ? await _uow.Notifications.GetAllAsync(n => n.UserId == userId && !n.IsRead)
                : await _uow.Notifications.GetAllAsync(n => n.UserId == userId);

            return _mapper.Map<IEnumerable<NotificationDto>>(
                notifications.OrderByDescending(n => n.SentAt));
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _uow.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int userId, List<int> notificationIds)
        {
            var notifications = await _uow.Notifications.GetAllAsync(
                n => n.UserId == userId && notificationIds.Contains(n.NotificationId));

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                _uow.Notifications.Update(notification);
            }

            await _uow.SaveAsync();

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
                notifications.Count(), userId);

            // Broadcast update qua SignalR
            await _hubContext.Clients.Group($"User_{userId}")
                .SendAsync("NotificationsRead", notificationIds);
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifications = await _uow.Notifications.GetAllAsync(
                n => n.UserId == userId && !n.IsRead);

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                _uow.Notifications.Update(notification);
            }

            await _uow.SaveAsync();

            _logger.LogInformation("Marked all notifications as read for user {UserId}", userId);

            // Broadcast update qua SignalR
            await _hubContext.Clients.Group($"User_{userId}")
                .SendAsync("AllNotificationsRead");
        }

        public async Task DeleteNotificationAsync(int notificationId, int userId)
        {
            var notification = await _uow.Notifications.FirstOrDefaultAsync(
                n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification != null)
            {
                _uow.Notifications.Remove(notification);
                await _uow.SaveAsync();

                _logger.LogInformation("Deleted notification {NotificationId}", notificationId);
            }
        }
    }
}
