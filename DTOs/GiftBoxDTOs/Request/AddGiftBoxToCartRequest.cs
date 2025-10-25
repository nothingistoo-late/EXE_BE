using System.ComponentModel.DataAnnotations;

namespace DTOs.GiftBoxDTOs.Request
{
    public class AddGiftBoxToCartRequest
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
        [MaxLength(1000)]
        public string BoxDescription { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string LetterScription { get; set; } = string.Empty;
        
        [Required]
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
        public int Quantity { get; set; } = 1;
    }
}
