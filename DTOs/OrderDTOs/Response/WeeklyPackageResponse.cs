using System;

namespace DTOs.OrderDTOs.Respond
{
    /// <summary>
    /// Response DTO cho gói hàng tuần
    /// Chứa thông tin của cả 2 đơn hàng trong gói
    /// </summary>
    public class WeeklyPackageResponse
    {
        public Guid WeeklyPackageId { get; set; }  // ID nhóm gói hàng tuần
        public double TotalPackagePrice { get; set; }  // Tổng giá trị gói (250k)
        public double Savings { get; set; }  // Số tiền tiết kiệm được (50k)
        public DateTime DeliveryStartDate { get; set; }  // Ngày giao hàng đầu tiên
        public DateTime SecondDeliveryDate { get; set; }  // Ngày giao hàng thứ hai (cách 3 ngày)
        public List<WeeklyOrderResponse> Orders { get; set; } = new();  // Danh sách 2 đơn hàng
    }

    /// <summary>
    /// Response DTO cho từng đơn hàng trong gói hàng tuần
    /// </summary>
    public class WeeklyOrderResponse
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime ScheduledDeliveryDate { get; set; }  // Ngày giao hàng dự kiến
        public double TotalPrice { get; set; }
        public double FinalPrice { get; set; }
        public string? DiscountCode { get; set; }
        public string Address { get; set; } = string.Empty;
        public string DeliveryTo { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public bool IsWeeklyPackage { get; set; } = true;
        public Guid WeeklyPackageId { get; set; }
        public List<OrderDetailResponse> Details { get; set; } = new();
    }
}
