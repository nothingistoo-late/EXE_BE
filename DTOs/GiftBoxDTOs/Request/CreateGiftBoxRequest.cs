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
        [MaxLength(100)]
        public string Receiver { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Occasion { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string MainWish { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? CustomMessage { get; set; }
        
        [Required]
        public int Quantity { get; set; } = 1;
        
        public string? DiscountCode { get; set; }
        
        [Required]
        public BusinessObjects.Common.DeliveryMethod DeliveryMethod { get; set; }
        
        [Required]
        public BusinessObjects.Common.PaymentMethod PaymentMethod { get; set; }
    }
}

