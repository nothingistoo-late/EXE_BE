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
        public string Address { get; set; } = null!;  // Địa chỉ giao hàng
        public string DeliveryTo { get; set; } = null!;  // Tên người nhận
        public string PhoneNumber { get; set; } = null!;  // Số điện thoại người nhận
        
        // Weekly Package fields
        public bool IsWeeklyPackage { get; set; } = false;  // Đánh dấu đơn hàng thuộc gói hàng tuần
        public Guid? WeeklyPackageId { get; set; }  // ID nhóm các đơn hàng trong gói hàng tuần
        public DateTime? ScheduledDeliveryDate { get; set; }  // Ngày giao hàng dự kiến cho đơn hàng trong gói
        
        // PayOS fields
        public string? PayOSPaymentLinkId { get; set; }
        public string? PayOSPaymentUrl { get; set; }
        public string? PayOSOrderCode { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

}
