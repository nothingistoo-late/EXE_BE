using System.ComponentModel.DataAnnotations;

namespace DTOs.GiftBoxDTOs.Request
{
    public class CreateGiftBoxRequest
    {
        [Required(ErrorMessage = "UserId là bắt buộc")]
        public Guid UserId { get; set; }
        
        [Required(ErrorMessage = "Danh sách rau củ là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 loại rau củ")]
        public List<string> Vegetables { get; set; } = new List<string>();
        
        [Required(ErrorMessage = "Lời chúc là bắt buộc")]
        [MaxLength(500, ErrorMessage = "Lời chúc không được vượt quá 500 ký tự")]
        public string GreetingMessage { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mô tả hộp quà là bắt buộc")]
        [MaxLength(1000, ErrorMessage = "Mô tả hộp quà không được vượt quá 1000 ký tự")]
        public string BoxDescription { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Nội dung thư là bắt buộc")]
        [MaxLength(1000, ErrorMessage = "Nội dung thư không được vượt quá 1000 ký tự")]
        public string LetterScription { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; } = 1;
        
        public string? DiscountCode { get; set; }
        
        [Required(ErrorMessage = "Phương thức giao hàng là bắt buộc")]
        public BusinessObjects.Common.DeliveryMethod DeliveryMethod { get; set; }
        
        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
        public BusinessObjects.Common.PaymentMethod PaymentMethod { get; set; }
        
        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        [StringLength(1000, ErrorMessage = "Địa chỉ không được vượt quá 1000 ký tự")]
        public string Address { get; set; } = string.Empty;  // Địa chỉ giao hàng
        
        [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự")]
        public string DeliveryTo { get; set; } = string.Empty;  // Tên người nhận
        
        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [RegularExpression(@"^(0|\+84)[1-9][0-9]{8,9}$", ErrorMessage = "Số điện thoại không hợp lệ. Ví dụ: 0912345678 hoặc +84912345678")]
        public string PhoneNumber { get; set; } = string.Empty;  // Số điện thoại người nhận
    }
}

