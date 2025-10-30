using System;

namespace BusinessObjects
{
    /// <summary>
    /// Entity tracking số lần đã giao trong mỗi tuần
    /// Mỗi tuần có 1 record, tracking 2 lần giao hàng
    /// </summary>
    public class WeeklyDeliverySchedule : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid SubscriptionId { get; set; }
        public WeeklyBlindBoxSubscription Subscription { get; set; } = null!;

        // Tuần này (bắt đầu từ thứ 2)
        public DateTime WeekStartDate { get; set; } // Thứ 2 đầu tuần
        public DateTime WeekEndDate { get; set; } // Chủ nhật cuối tuần

        // Lần giao hàng 1 (thứ 2)
        public DateTime? FirstDeliveryDate { get; set; } // Ngày giao hàng dự kiến lần 1
        public bool IsFirstDelivered { get; set; } = false; // Đã giao lần 1 chưa
        public DateTime? FirstDeliveredAt { get; set; } // Thời gian thực tế giao lần 1

        // Lần giao hàng 2 (thứ 5)
        public DateTime? SecondDeliveryDate { get; set; } // Ngày giao hàng dự kiến lần 2
        public bool IsSecondDelivered { get; set; } = false; // Đã giao lần 2 chưa
        public DateTime? SecondDeliveredAt { get; set; } // Thời gian thực tế giao lần 2

        // Trạng thái tuần này
        public bool IsPaused { get; set; } = false; // Tuần này có bị hoãn không (admin pause)
        public string? PauseReason { get; set; } // Lý do hoãn

        // Notes
        public string? Note { get; set; }
    }
}

