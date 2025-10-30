using BusinessObjects.Common;
using System;
using System.Collections.Generic;

namespace BusinessObjects
{
    /// <summary>
    /// Entity lưu thông tin đăng ký gói BlindBox theo tuần
    /// 1 tuần giao 2 lần, giá ưu đãi
    /// </summary>
    public class WeeklyBlindBoxSubscription : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid BoxTypeId { get; set; } // ID của Blind Box
        public BoxTypes BoxType { get; set; } = null!;

        // Thông tin gói
        public DateTime StartDate { get; set; } // Ngày bắt đầu gói
        public DateTime EndDate { get; set; } // Ngày kết thúc gói
        public int DurationWeeks { get; set; } // Số tuần đăng ký (mặc định 4 tuần)

        // Giá cả
        public double WeeklyPrice { get; set; } // Giá gói/tuần (rẻ hơn 2 lần mua lẻ)
        public double TotalPrice { get; set; } // Tổng giá của gói
        public double PerBoxPrice { get; set; } // Giá mỗi box trong gói

        // Thanh toán
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? PayOSOrderCode { get; set; }

        // Lịch giao hàng (mặc định: Thứ 2 và Thứ 5)
        public DayOfWeek FirstDeliveryDay { get; set; } = DayOfWeek.Monday;
        public DayOfWeek SecondDeliveryDay { get; set; } = DayOfWeek.Thursday;

        // Trạng thái
        public WeeklySubscriptionStatus Status { get; set; } = WeeklySubscriptionStatus.Active;

        // Địa chỉ giao hàng
        public string Address { get; set; } = null!;
        public string DeliveryTo { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        // Notes
        public string? AllergyNote { get; set; }
        public string? PreferenceNote { get; set; }

        // Quan hệ với delivery schedules
        public ICollection<WeeklyDeliverySchedule> DeliverySchedules { get; set; } = new List<WeeklyDeliverySchedule>();
    }

    /// <summary>
    /// Enum trạng thái subscription
    /// </summary>
    public enum WeeklySubscriptionStatus
    {
        Active = 1,      // Đang hoạt động
        Paused = 2,     // Tạm hoãn (user yêu cầu hoãn tuần này)
        Expired = 3,    // Hết hạn
        Cancelled = 4   // Đã hủy
    }
}

