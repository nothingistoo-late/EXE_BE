using System.ComponentModel.DataAnnotations;

namespace DTOs.AiMenuDTOs.Request
{
    public class GetUserRecipesRequest
    {
        [Range(1, 1000, ErrorMessage = "Count must be between 1 and 1000")]
        public int Count { get; set; } = 50;

        public string? SearchTerm { get; set; } // Search by dish name

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
