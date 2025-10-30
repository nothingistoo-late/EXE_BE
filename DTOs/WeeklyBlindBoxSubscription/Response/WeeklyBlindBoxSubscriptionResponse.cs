using System;
using System.Collections.Generic;

namespace DTOs.WeeklyBlindBoxSubscription.Response
{
    public class WeeklyBlindBoxSubscriptionResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        public Guid BoxTypeId { get; set; }
        public string BoxTypeName { get; set; } = string.Empty;
        public double BoxTypePrice { get; set; }

        // Thông tin gói
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationWeeks { get; set; }
        public int RemainingWeeks { get; set; } // Số tuần còn lại

        // Giá cả
        public double WeeklyPrice { get; set; }
        public double TotalPrice { get; set; }
        public double PerBoxPrice { get; set; }
        public double SavingsPerWeek { get; set; } // Tiết kiệm so với mua lẻ

        // Thanh toán
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;

        // Lịch giao hàng
        public string FirstDeliveryDay { get; set; } = string.Empty;
        public string SecondDeliveryDay { get; set; } = string.Empty;

        // Trạng thái
        public string Status { get; set; } = string.Empty;

        // Địa chỉ
        public string Address { get; set; } = string.Empty;
        public string DeliveryTo { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // Notes
        public string? AllergyNote { get; set; }
        public string? PreferenceNote { get; set; }

        // Thông tin delivery schedules
        public List<WeeklyDeliveryScheduleResponse> DeliverySchedules { get; set; } = new List<WeeklyDeliveryScheduleResponse>();
    }

    public class WeeklyDeliveryScheduleResponse
    {
        public Guid Id { get; set; }
        public Guid SubscriptionId { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }

        // Lần giao hàng 1
        public DateTime? FirstDeliveryDate { get; set; }
        public bool IsFirstDelivered { get; set; }
        public DateTime? FirstDeliveredAt { get; set; }

        // Lần giao hàng 2
        public DateTime? SecondDeliveryDate { get; set; }
        public bool IsSecondDelivered { get; set; }
        public DateTime? SecondDeliveredAt { get; set; }

        // Trạng thái
        public bool IsPaused { get; set; }
        public string? PauseReason { get; set; }

        // Tổng số lần đã giao trong tuần này
        public int DeliveryCount { get; set; } // 0, 1, hoặc 2

        // Thông tin subscription (optional - chỉ có khi admin query)
        public SubscriptionInfo? SubscriptionInfo { get; set; }
    }

    public class SubscriptionInfo
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}

