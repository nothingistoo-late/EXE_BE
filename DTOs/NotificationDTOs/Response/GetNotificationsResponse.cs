using BusinessObjects.Common;

namespace DTOs.NotificationDTOs.Response
{
    /// <summary>
    /// Response for getting notifications list
    /// </summary>
    public class GetNotificationsResponse
    {
        public List<UserNotificationResponse> Items { get; set; }
        public int UnreadCount { get; set; }

        public GetNotificationsResponse(List<UserNotificationResponse> items, int unreadCount)
        {
            Items = items;
            UnreadCount = unreadCount;
        }
    }

    /// <summary>
    /// Response for admin getting notifications list
    /// </summary>
    public class GetAdminNotificationsResponse
    {
        public List<AdminNotificationResponse> Items { get; set; }
        public int TotalNotifications { get; set; }
        public int UnreadCount { get; set; }
        public int BroadcastCount { get; set; }

        public GetAdminNotificationsResponse(List<AdminNotificationResponse> items, int totalNotifications, int unreadCount, int broadcastCount)
        {
            Items = items;
            TotalNotifications = totalNotifications;
            UnreadCount = unreadCount;
            BroadcastCount = broadcastCount;
        }
    }
}