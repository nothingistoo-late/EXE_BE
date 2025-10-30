using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.WeeklyBlindBoxSubscription.Request
{
    public class PauseWeekDeliveryRequest
    {
        [Required(ErrorMessage = "SubscriptionId không được để trống")]
        public Guid SubscriptionId { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu tuần không được để trống")]
        public DateTime WeekStartDate { get; set; } // Thứ 2 đầu tuần cần hoãn

        public string? Reason { get; set; } // Lý do hoãn
    }
}

