using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.OrderDTOs.Request
{
    public class CreateOrderRequest
    {
        public Guid UserId { get; set; }
        public List<CreateOrderDetailRequest> Items { get; set; } = new();
        public string? DiscountCode { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Address { get; set; } = null!;  // Địa chỉ giao hàng
        public string DeliveryTo { get; set; } = null!;  // Tên người nhận
        public string PhoneNumber { get; set; } = null!;  // Số điện thoại người nhận
    }

    public class CreateOrderDetailRequest
    {
        public Guid BoxTypeId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateOrderRequest
    {
        public OrderStatus Status { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? DiscountCode { get; set; }
        public string Address { get; set; } = null!;  // Địa chỉ giao hàng
        public string DeliveryTo { get; set; } = null!;  // Tên người nhận
        public string PhoneNumber { get; set; } = null!;  // Số điện thoại người nhận
        public List<CreateOrderDetailRequest> Items { get; set; } = new();
    }

}
