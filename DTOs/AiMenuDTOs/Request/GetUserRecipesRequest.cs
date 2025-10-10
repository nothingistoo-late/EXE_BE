using System.ComponentModel.DataAnnotations;

namespace DTOs.AiMenuDTOs.Request
{
    public class GetUserRecipesRequest
    {
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        public string? SearchTerm { get; set; } // Search by dish name

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
