using System.ComponentModel.DataAnnotations;

namespace DTOs.GiftBoxDTOs.Request
{
    public class CreateGiftBoxRequest
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [MinLength(1, ErrorMessage = "At least one vegetable is required")]
        public List<string> Vegetables { get; set; } = new List<string>();
        
        [Required]
        [MaxLength(500)]
        public string GreetingMessage { get; set; } = string.Empty;
        
        [Required]
        public int Quantity { get; set; } = 1;
        
        public string? DiscountCode { get; set; }
        
        [Required]
        public BusinessObjects.Common.DeliveryMethod DeliveryMethod { get; set; }
        
        [Required]
        public BusinessObjects.Common.PaymentMethod PaymentMethod { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string Address { get; set; } = string.Empty;  // Địa chỉ giao hàng
    }
}

