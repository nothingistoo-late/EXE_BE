namespace DTOs.AiMenuDTOs.Response
{
    public class AdminGenerateRecipeResponse
    {
        public Guid Id { get; set; }
        public string DishName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Ingredients { get; set; } = new List<string>();
        public List<string> Instructions { get; set; } = new List<string>();
        public string EstimatedCookingTime { get; set; } = string.Empty;
        public string? CookingTips { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
