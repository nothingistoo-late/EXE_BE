using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class GiftBoxOrder : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid OrderId { get; set; }
        
        [Required]
        public string Vegetables { get; set; } = string.Empty; // JSON string of vegetables list
        
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
        [MaxLength(1000)]
        public string Address { get; set; } = string.Empty;  // Địa chỉ giao hàng cho gift box
        
        // Navigation properties
        public virtual Order Order { get; set; } = null!;
        public virtual Review? Review { get; set; }
    }
}
