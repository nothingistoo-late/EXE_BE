namespace DTOs.ReviewDTOs.Response
{
    public class ReviewDetailResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public int ServiceQualityRating { get; set; }
        public int ProductQualityRating { get; set; }
        public string? ReviewContent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Order details
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
    }
}

