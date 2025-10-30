using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.WeeklyBlindBoxSubscription.Request
{
    public class ResumeDeliveryRequest
    {
        [Required(ErrorMessage = "ScheduleId không được để trống")]
        public Guid ScheduleId { get; set; }

        /// <summary>
        /// Ngày giao hàng mới cho lần 1 (nếu null thì giữ nguyên FirstDeliveryDate)
        /// </summary>
        public DateTime? NewFirstDeliveryDate { get; set; }

        /// <summary>
        /// Ngày giao hàng mới cho lần 2 (nếu null thì giữ nguyên SecondDeliveryDate)
        /// </summary>
        public DateTime? NewSecondDeliveryDate { get; set; }
    }
}

