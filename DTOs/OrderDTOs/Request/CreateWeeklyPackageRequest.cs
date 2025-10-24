using BusinessObjects.Common;
using System.ComponentModel.DataAnnotations;

namespace DTOs.OrderDTOs.Request
{
    /// <summary>
    /// Request DTO cho việc tạo gói hàng tuần
    /// Gói hàng tuần bao gồm 2 lần giao hàng với giá ưu đãi 250k thay vì 300k
    /// </summary>
    public class CreateWeeklyPackageRequest
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public List<CreateOrderDetailRequest> Items { get; set; } = new();
        
        public string? DiscountCode { get; set; }
        
        [Required]
        public DeliveryMethod DeliveryMethod { get; set; }
        
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string Address { get; set; } = null!;  // Địa chỉ giao hàng
        
        [Required]
        [MaxLength(100)]
        public string DeliveryTo { get; set; } = null!;  // Tên người nhận
        
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = null!;  // Số điện thoại người nhận
        
        // Allergy and Preference Notes
        [MaxLength(500)]
        public string? AllergyNote { get; set; }  // Ghi chú về dị ứng thực phẩm
        
        [MaxLength(500)]
        public string? PreferenceNote { get; set; }  // Ghi chú về sở thích ăn uống
        
        [Required]
        public DateTime DeliveryStartDate { get; set; }  // Ngày bắt đầu giao hàng (đơn hàng đầu tiên)
        
        /// <summary>
        /// Tổng giá trị gói hàng tuần (250k thay vì 300k)
        /// </summary>
        public double WeeklyPackagePrice { get; set; } = 250000;
    }
}
