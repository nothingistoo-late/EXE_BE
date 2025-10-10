using BusinessObjects.Common;

namespace DTOs.NotificationDTOs.Response
{
    /// <summary>
    /// Base notification response
    /// </summary>
    public abstract class BaseNotificationResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Notification response for regular users (simplified)
    /// </summary>
    public class UserNotificationResponse : BaseNotificationResponse
    {
        public string SenderName { get; set; }
        public bool IsBroadcast { get; set; } // true nếu là broadcast notification
    }

    /// <summary>
    /// Notification response for admin (with full details)
    /// </summary>
    public class AdminNotificationResponse : BaseNotificationResponse
    {
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }
        public Guid? ReceiverId { get; set; }
        public string? ReceiverName { get; set; }
        public bool IsBroadcast { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
