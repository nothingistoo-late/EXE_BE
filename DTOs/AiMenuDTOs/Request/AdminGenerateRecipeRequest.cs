using System.ComponentModel.DataAnnotations;

namespace DTOs.AiMenuDTOs.Request
{
    public class AdminGenerateRecipeRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one ingredient is required")]
        public List<string> Ingredients { get; set; } = new List<string>();

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(200)]
        public string? DietaryPreferences { get; set; } // e.g., "vegetarian", "vegan", "gluten-free"

        [MaxLength(200)]
        public string? CookingSkillLevel { get; set; } // e.g., "beginner", "intermediate", "advanced"
    }
}
