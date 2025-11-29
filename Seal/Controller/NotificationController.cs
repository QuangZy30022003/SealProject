using Common.DTOs.NotificationDto;
using Common.Wrappers;
using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace Seal.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUserContextService _userContext;

        public NotificationController(
            INotificationService notificationService,
            IUserContextService userContext)
        {
            _notificationService = notificationService;
            _userContext = userContext;
        }

        /// <summary>
        /// Lấy tất cả notifications của user hiện tại
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications([FromQuery] bool unreadOnly = false)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
            return Ok(ApiResponse<IEnumerable<NotificationDto>>.Ok(result));
        }

        /// <summary>
        /// Đếm số notifications chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userContext.GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(ApiResponse<int>.Ok(count));
        }

        /// <summary>
        /// Đánh dấu notifications đã đọc
        /// </summary>
        [HttpPut("mark-read")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadDto dto)
        {
            var userId = _userContext.GetCurrentUserId();
            await _notificationService.MarkAsReadAsync(userId, dto.NotificationIds);
            return Ok(ApiResponse<object>.Ok(null, "Notifications marked as read"));
        }

        /// <summary>
        /// Đánh dấu tất cả đã đọc
        /// </summary>
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userContext.GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(ApiResponse<object>.Ok(null, "All notifications marked as read"));
        }

        /// <summary>
        /// Xóa notification
        /// </summary>
        [HttpDelete("{notificationId}")]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            var userId = _userContext.GetCurrentUserId();
            await _notificationService.DeleteNotificationAsync(notificationId, userId);
            return Ok(ApiResponse<object>.Ok(null, "Notification deleted"));
        }
    }
}
