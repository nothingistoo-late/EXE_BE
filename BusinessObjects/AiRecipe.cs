using System.ComponentModel.DataAnnotations;

namespace BusinessObjects
{
    public class AiRecipe : BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string DishName { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Ingredients { get; set; } = string.Empty; // JSON string of ingredients array

        [Required]
        public string Instructions { get; set; } = string.Empty; // JSON string of instructions array

        [Required]
        [MaxLength(50)]
        public string EstimatedCookingTime { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CookingTips { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        public string InputVegetables { get; set; } = string.Empty; // JSON string of input vegetables array

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AiModel { get; set; } = string.Empty; // e.g., "OpenAI", "Gemini"

        [Required]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
