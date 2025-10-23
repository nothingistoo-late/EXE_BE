using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects
{
    public class Review : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid OrderId { get; set; }
        
        [Required]
        [Range(1, 5, ErrorMessage = "Service quality rating must be between 1 and 5")]
        public int ServiceQualityRating { get; set; }
        
        [Required]
        [Range(1, 5, ErrorMessage = "Product quality rating must be between 1 and 5")]
        public int ProductQualityRating { get; set; }
        
        [MaxLength(1000)]
        public string ReviewContent { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual Order Order { get; set; } = null!;
    }
}

