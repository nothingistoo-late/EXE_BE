using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.WeeklyBlindBoxSubscription.Request
{
    public class MarkDeliveryRequest
    {
        [Required(ErrorMessage = "ScheduleId không được để trống")]
        public Guid ScheduleId { get; set; }

        [Required(ErrorMessage = "DeliveryNumber không được để trống")]
        [Range(1, 2, ErrorMessage = "DeliveryNumber phải là 1 hoặc 2")]
        public int DeliveryNumber { get; set; } // 1 = lần giao đầu tiên, 2 = lần giao thứ hai

        public DateTime? DeliveredAt { get; set; } // Nếu null thì dùng thời gian hiện tại
    }
}

