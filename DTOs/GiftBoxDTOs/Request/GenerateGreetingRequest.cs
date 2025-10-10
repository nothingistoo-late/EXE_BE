using System.ComponentModel.DataAnnotations;

namespace DTOs.GiftBoxDTOs.Request
{
    public class GenerateGreetingRequest
    {
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
    }
}

