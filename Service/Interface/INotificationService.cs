using Common.DTOs.NotificationDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface INotificationService
    {
        // Tạo notification cho 1 user
        Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto);

        // Tạo notification cho nhiều users
        Task CreateNotificationsAsync(List<int> userIds, string message);

        // Lấy notifications của user
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);

        // Đếm notifications chưa đọc
        Task<int> GetUnreadCountAsync(int userId);

        // Đánh dấu đã đọc
        Task MarkAsReadAsync(int userId, List<int> notificationIds);

        // Đánh dấu tất cả đã đọc
        Task MarkAllAsReadAsync(int userId);

        // Xóa notification
        Task DeleteNotificationAsync(int notificationId, int userId);
    }
}
