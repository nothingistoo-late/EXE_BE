using System;
using System.Collections.Generic;

namespace DTOs.PayOSDTOs
{
    public class CreatePaymentLinkRequest
    {
        public Guid OrderId { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<PaymentItem> Items { get; set; } = new();
    }

    public class PaymentItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Price { get; set; }
    }
}




