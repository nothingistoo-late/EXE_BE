using System.ComponentModel.DataAnnotations;

namespace DTOs.WeeklyBlindBoxSubscription.Request
{
    public class RenewWeeklyBlindBoxSubscriptionRequest
    {
        [Required(ErrorMessage = "SubscriptionId không được để trống")]
        public Guid SubscriptionId { get; set; }

        [Range(1, 52, ErrorMessage = "Số tuần gia hạn phải từ 1 đến 52")]
        public int AdditionalWeeks { get; set; } = 4;
    }
}

