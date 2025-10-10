namespace DTOs.GiftBoxDTOs.Response
{
    public class GiftBoxOrderResponse
    {
        public Guid OrderId { get; set; }
        public Guid GiftBoxOrderId { get; set; }
        public List<string> Vegetables { get; set; } = new List<string>();
        public string Receiver { get; set; } = string.Empty;
        public string Occasion { get; set; } = string.Empty;
        public string GreetingMessage { get; set; } = string.Empty;
        public double TotalPrice { get; set; }
        public double FinalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

