using BusinessObjects;
using BusinessObjects.Common;
using DTOs.NotificationDTOs.Request;
using DTOs.NotificationDTOs.Response;

namespace Repositories.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification, Guid>
    {
        Task<List<UserNotificationResponse>> GetNotificationsAsync(GetNotificationsRequest request, Guid userId);
        Task<List<AdminNotificationResponse>> GetNotificationsForAdminAsync(GetNotificationsRequest request);
        Task<Notification?> GetNotificationByIdAsync(Guid id);
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<List<Notification>> GetNotificationsForUserAsync(Guid userId, int count = 10);
        Task<List<Notification>> GetBroadcastNotificationsAsync(int count = 10);
        Task<bool> ExistsAsync(Guid notificationId);
    }
}
