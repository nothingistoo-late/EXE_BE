using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.OrderDTOs.Respond
{
    public class OrderResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; } 
        public double TotalPrice { get; set; }
        public double FinalPrice { get; set; }
        public string? DiscountCode { get; set; }
        public List<OrderDetailResponse> Details { get; set; } = new();
    }

    public class OrderDetailResponse
    {
        public Guid BoxTypeId { get; set; }
        public string BoxName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
    }

}
