using System.ComponentModel.DataAnnotations;

namespace DTOs.ReviewDTOs.Request
{
    public class UpdateReviewRequest
    {
        [Required]
        public Guid Id { get; set; }
        
        [Required]
        [Range(1, 5, ErrorMessage = "Service quality rating must be between 1 and 5")]
        public int ServiceQualityRating { get; set; }
        
        [Required]
        [Range(1, 5, ErrorMessage = "Product quality rating must be between 1 and 5")]
        public int ProductQualityRating { get; set; }
        
        [MaxLength(1000, ErrorMessage = "Review content cannot exceed 1000 characters")]
        public string? ReviewContent { get; set; }
    }
}

