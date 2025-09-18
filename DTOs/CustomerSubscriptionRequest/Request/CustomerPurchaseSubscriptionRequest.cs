using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CustomerSubscriptionRequest.Request
{
    public class CustomerPurchaseSubscriptionRequest
    {
        public Guid CustomerId { get; set; }
        public Guid SubscriptionPackageId { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }

        // Thông tin phiếu sức khoẻ
        public string? Allergy { get; set; }
        public string? Feeling { get; set; }
    }
}
