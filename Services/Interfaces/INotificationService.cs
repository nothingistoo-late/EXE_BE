using BusinessObjects.Common;
using DTOs.NotificationDTOs.Request;
using DTOs.NotificationDTOs.Response;

namespace Services.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResult<SendToUserNotificationResponse>> SendToUserAsync(SendToUserNotificationRequest request, Guid senderId);
        Task<ApiResult<SendToMultipleUsersNotificationResponse>> SendToMultipleUsersAsync(SendToMultipleUsersNotificationRequest request, Guid senderId);
        Task<ApiResult<SendBroadcastNotificationResponse>> SendBroadcastAsync(SendBroadcastNotificationRequest request, Guid senderId);
        Task<ApiResult<GetNotificationsResponse>> GetNotificationsAsync(GetNotificationsRequest request, Guid userId);
        Task<ApiResult<GetAdminNotificationsResponse>> GetNotificationsForAdminAsync(GetNotificationsRequest request);
        Task<ApiResult<UserNotificationResponse>> GetNotificationByIdAsync(Guid notificationId, Guid userId);
        Task<ApiResult<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);
        Task<ApiResult<bool>> MarkAllAsReadAsync(Guid userId);
        Task<ApiResult<int>> GetUnreadCountAsync(Guid userId);
        Task<ApiResult<List<UserNotificationResponse>>> GetRecentNotificationsAsync(Guid userId, int count = 10);
        Task<ApiResult<bool>> DeleteNotificationAsync(Guid notificationId, Guid userId);
    }
}
