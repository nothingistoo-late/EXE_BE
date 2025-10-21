namespace DTOs.ReviewDTOs.Response
{
    public class ReviewDetailResponse
    {
        public Guid Id { get; set; }
        public Guid GiftBoxOrderId { get; set; }
        public int ServiceQualityRating { get; set; }
        public int ProductQualityRating { get; set; }
        public string? ReviewContent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // GiftBoxOrder details
        public string OrderId { get; set; } = string.Empty;
        public string GreetingMessage { get; set; } = string.Empty;
        public string BoxDescription { get; set; } = string.Empty;
        public string LetterScription { get; set; } = string.Empty;
    }
}

