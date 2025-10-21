namespace DTOs.ReviewDTOs.Response
{
    public class ReviewResponse
    {
        public Guid Id { get; set; }
        public Guid GiftBoxOrderId { get; set; }
        public int ServiceQualityRating { get; set; }
        public int ProductQualityRating { get; set; }
        public string? ReviewContent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

