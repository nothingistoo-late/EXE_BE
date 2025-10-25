namespace DTOs.GiftBoxDTOs.Response
{
    public class AddGiftBoxToCartResponse
    {
        public Guid CartId { get; set; }
        public Guid GiftBoxOrderId { get; set; }
        public List<string> Vegetables { get; set; } = new List<string>();
        public string GreetingMessage { get; set; } = string.Empty;
        public string BoxDescription { get; set; } = string.Empty;
        public string LetterScription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
