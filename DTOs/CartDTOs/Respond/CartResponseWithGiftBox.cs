using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CartDTOs.Respond
{
    public class CartResponseWithGiftBox
    {
        public Guid Id { get; set; }
        public double TotalPrice { get; set; }
        public double FinalPrice { get; set; }
        public List<CartItemWithGiftBoxResponse> Items { get; set; } = new();
    }
}
