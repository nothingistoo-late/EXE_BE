using System.ComponentModel.DataAnnotations;

namespace DTOs.NotificationDTOs.Request
{
    public class MarkAsReadRequest
    {
        [Required(ErrorMessage = "Notification ID is required")]
        public Guid NotificationId { get; set; }
    }
}
