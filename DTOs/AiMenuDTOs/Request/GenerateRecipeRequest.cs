using System.ComponentModel.DataAnnotations;

namespace DTOs.AiMenuDTOs.Request
{
    public class GenerateRecipeRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one vegetable is required")]
        public List<string> Vegetables { get; set; } = new List<string>();

        [MaxLength(200)]
        public string? DietaryPreferences { get; set; } // e.g., "vegetarian", "vegan", "gluten-free"

        [MaxLength(200)]
        public string? CookingSkillLevel { get; set; } // e.g., "beginner", "intermediate", "advanced"
    }
}
