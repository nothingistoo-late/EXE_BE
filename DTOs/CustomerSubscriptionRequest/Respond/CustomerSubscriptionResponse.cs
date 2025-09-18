using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CustomerSubscriptionRequest.Respond
{
    public class CustomerSubscriptionResponse
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public Guid SubscriptionPackageId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }

        // Thông tin phiếu sức khoẻ
        public Guid HealthSurveyId { get; set; }
        public string? Allergy { get; set; }
        public string? Feeling { get; set; }
    }
}
