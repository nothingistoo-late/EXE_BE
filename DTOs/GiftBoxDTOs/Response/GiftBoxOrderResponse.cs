namespace DTOs.GiftBoxDTOs.Response
{
    public class GiftBoxOrderResponse
    {
        public Guid OrderId { get; set; }
        public Guid GiftBoxOrderId { get; set; }
        public List<string> Vegetables { get; set; } = new List<string>();
        public string GreetingMessage { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;  // Địa chỉ giao hàng
        public double TotalPrice { get; set; }
        public double FinalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

