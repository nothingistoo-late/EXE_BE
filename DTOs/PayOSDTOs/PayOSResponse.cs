using System;

namespace DTOs.PayOSDTOs
{
    public class PaymentLinkResponse
    {
        public string PaymentLinkId { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public long OrderCode { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}




