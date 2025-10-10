using BusinessObjects.Common;

namespace DTOs.NotificationDTOs.Response
{
    /// <summary>
    /// Base response for notification sending
    /// </summary>
    public abstract class BaseSendNotificationResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Response for sending notification to a specific user
    /// </summary>
    public class SendToUserNotificationResponse : BaseSendNotificationResponse
    {
        public Guid ReceiverId { get; set; }
        public string ReceiverName { get; set; }
    }

    /// <summary>
    /// Response for sending notification to multiple users
    /// </summary>
    public class SendToMultipleUsersNotificationResponse : BaseSendNotificationResponse
    {
        public List<Guid> ReceiverIds { get; set; }
        public List<string> ReceiverNames { get; set; }
        public int TotalSent { get; set; }
    }

    /// <summary>
    /// Response for sending broadcast notification
    /// </summary>
    public class SendBroadcastNotificationResponse : BaseSendNotificationResponse
    {
        public int TotalUsers { get; set; } // Số lượng user nhận được notification
    }
}
