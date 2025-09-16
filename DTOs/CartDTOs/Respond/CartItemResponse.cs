using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CartDTOs.Respond
{
    public class CartItemResponse
    {
        public Guid Id { get; set; }
        public Guid BoxTypeId { get; set; }
        public string? BoxTypeName { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
    }
}
