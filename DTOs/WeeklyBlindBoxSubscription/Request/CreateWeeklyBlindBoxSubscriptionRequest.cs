using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.WeeklyBlindBoxSubscription.Request
{
    public class CreateWeeklyBlindBoxSubscriptionRequest
    {
        [Required(ErrorMessage = "BoxTypeId không được để trống")]
        public Guid BoxTypeId { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime StartDate { get; set; } // Ngày bắt đầu gói (phải là thứ 2)

        [Range(1, 52, ErrorMessage = "Số tuần phải từ 1 đến 52")]
        public int DurationWeeks { get; set; } = 4; // Mặc định 4 tuần

        [Required(ErrorMessage = "Phương thức thanh toán không được để trống")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên người nhận không được để trống")]
        public string DeliveryTo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? AllergyNote { get; set; }
        public string? PreferenceNote { get; set; }

        // Ngày giao hàng (tùy chọn, mặc định Thứ 2 và Thứ 5)
        public DayOfWeek FirstDeliveryDay { get; set; } = DayOfWeek.Monday;
        public DayOfWeek SecondDeliveryDay { get; set; } = DayOfWeek.Thursday;
    }
}

