using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.OrderDTOs.Request
{
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "UserId là bắt buộc")]
        public Guid UserId { get; set; }
        
        [Required(ErrorMessage = "Items là bắt buộc")]
        [MinLength(1, ErrorMessage = "Đơn đặt hàng phải có ít nhất 1 sản phẩm")]
        public List<CreateOrderDetailRequest> Items { get; set; } = new();
        
        public string? DiscountCode { get; set; }
        
        [Required(ErrorMessage = "Phương thức giao hàng là bắt buộc")]
        public DeliveryMethod DeliveryMethod { get; set; }
        
        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        public PaymentMethod PaymentMethod { get; set; }
        
        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        [StringLength(1000, ErrorMessage = "Địa chỉ không được vượt quá 1000 ký tự")]
        public string Address { get; set; } = null!;  // Địa chỉ giao hàng
        
        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự")]
        public string DeliveryTo { get; set; } = null!;  // Tên người nhận
        
        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [RegularExpression(@"^(0|\+84)[1-9][0-9]{8,9}$", ErrorMessage = "Số điện thoại không hợp lệ. Ví dụ: 0912345678 hoặc +84912345678")]
        public string PhoneNumber { get; set; } = null!;  // Số điện thoại người nhận
        
        // Allergy and Preference Notes
        [StringLength(500, ErrorMessage = "Ghi chú dị ứng không được vượt quá 500 ký tự")]
        public string? AllergyNote { get; set; }  // Ghi chú về dị ứng thực phẩm
        
        [StringLength(500, ErrorMessage = "Ghi chú sở thích không được vượt quá 500 ký tự")]
        public string? PreferenceNote { get; set; }  // Ghi chú về sở thích ăn uống
    }

    public class CreateOrderDetailRequest
    {
        [Required(ErrorMessage = "BoxTypeId là bắt buộc")]
        public Guid BoxTypeId { get; set; }
        
        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
    }

    public class UpdateOrderRequest
    {
        [Required(ErrorMessage = "Trạng thái đơn hàng là bắt buộc")]
        public OrderStatus Status { get; set; }
        
        [Required(ErrorMessage = "Phương thức giao hàng là bắt buộc")]
        public DeliveryMethod DeliveryMethod { get; set; }
        
        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        public PaymentMethod PaymentMethod { get; set; }
        
        public string? DiscountCode { get; set; }
        
        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        [StringLength(1000, ErrorMessage = "Địa chỉ không được vượt quá 1000 ký tự")]
        public string Address { get; set; } = null!;  // Địa chỉ giao hàng
        
        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự")]
        public string DeliveryTo { get; set; } = null!;  // Tên người nhận
        
        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [RegularExpression(@"^(0|\+84)[1-9][0-9]{8,9}$", ErrorMessage = "Số điện thoại không hợp lệ. Ví dụ: 0912345678 hoặc +84912345678")]
        public string PhoneNumber { get; set; } = null!;  // Số điện thoại người nhận
        
        // Allergy and Preference Notes
        [StringLength(500, ErrorMessage = "Ghi chú dị ứng không được vượt quá 500 ký tự")]
        public string? AllergyNote { get; set; }  // Ghi chú về dị ứng thực phẩm
        
        [StringLength(500, ErrorMessage = "Ghi chú sở thích không được vượt quá 500 ký tự")]
        public string? PreferenceNote { get; set; }  // Ghi chú về sở thích ăn uống
        
        [Required(ErrorMessage = "Items là bắt buộc")]
        [MinLength(1, ErrorMessage = "Đơn đặt hàng phải có ít nhất 1 sản phẩm")]
        public List<CreateOrderDetailRequest> Items { get; set; } = new();
    }

}
