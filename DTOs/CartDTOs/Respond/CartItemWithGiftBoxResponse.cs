using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CartDTOs.Respond
{
    public class CartItemWithGiftBoxResponse : CartItemResponse
    {
        // GiftBox specific fields (only populated when BoxType is GiftBox)
        public List<string>? Vegetables { get; set; }
        public string? GreetingMessage { get; set; }
        public string? BoxDescription { get; set; }
        public string? LetterScription { get; set; }
        public Guid? GiftBoxOrderId { get; set; }
    }
}
