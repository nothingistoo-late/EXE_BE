using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Order : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public OrderStatus Status { get; set; }
        public bool IsPaid { get; set; } = false;
        public bool IsDelivered { get; set; } = false;
        public DeliveryMethod DeliveryMethod { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public double TotalPrice { get; set; }
        public double FinalPrice { get; set; }  // sau khi áp dụng giảm giá
        public string? DiscountCode { get; set; }
        
        // PayOS fields
        public string? PayOSPaymentLinkId { get; set; }
        public string? PayOSPaymentUrl { get; set; }
        public string? PayOSOrderCode { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

}
