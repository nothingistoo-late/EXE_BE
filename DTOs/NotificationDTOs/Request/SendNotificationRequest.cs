using BusinessObjects.Common;
using System.ComponentModel.DataAnnotations;

namespace DTOs.NotificationDTOs.Request
{
    /// <summary>
    /// Base class for notification requests
    /// </summary>
    public abstract class BaseNotificationRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required")]
        [MaxLength(1000, ErrorMessage = "Content cannot exceed 1000 characters")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Type is required")]
        public NotificationType Type { get; set; }
    }

    /// <summary>
    /// Send notification to a specific user
    /// </summary>
    public class SendToUserNotificationRequest : BaseNotificationRequest
    {
        [Required(ErrorMessage = "ReceiverId is required")]
        public Guid ReceiverId { get; set; }
    }

    /// <summary>
    /// Send notification to multiple users
    /// </summary>
    public class SendToMultipleUsersNotificationRequest : BaseNotificationRequest
    {
        [Required(ErrorMessage = "ReceiverIds is required")]
        [MinLength(1, ErrorMessage = "At least one receiver is required")]
        public List<Guid> ReceiverIds { get; set; }
    }

    /// <summary>
    /// Send broadcast notification to all users
    /// </summary>
    public class SendBroadcastNotificationRequest : BaseNotificationRequest
    {
        // No additional properties needed for broadcast
    }
}
