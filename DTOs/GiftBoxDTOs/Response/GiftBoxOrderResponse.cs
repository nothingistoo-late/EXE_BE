namespace DTOs.GiftBoxDTOs.Response
{
    public class GiftBoxOrderResponse
    {
        public Guid OrderId { get; set; }
        public Guid GiftBoxOrderId { get; set; }
        public List<string> Vegetables { get; set; } = new List<string>();
        public string GreetingMessage { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;  // Địa chỉ giao hàng
        public string DeliveryTo { get; set; } = string.Empty;  // Tên người nhận
        public string PhoneNumber { get; set; } = string.Empty;  // Số điện thoại người nhận
        public double TotalPrice { get; set; }
        public double FinalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

