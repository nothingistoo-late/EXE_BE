using System.ComponentModel.DataAnnotations;

namespace DTOs.NotificationDTOs.Request
{
    public class GetNotificationsRequest
    {
        [Range(1, 1000, ErrorMessage = "Count must be between 1 and 1000")]
        public int Count { get; set; } = 50;

        public bool? IsRead { get; set; }

        public BusinessObjects.Common.NotificationType? Type { get; set; }
    }
}
