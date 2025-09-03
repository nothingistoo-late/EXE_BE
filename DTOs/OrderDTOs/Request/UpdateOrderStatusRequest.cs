using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.OrderDTOs.Request
{
    public class UpdateOrderStatusRequest
    {
        public List<Guid> OrderIds { get; set; } = new();
        public OrderStatus Status { get; set; }
    }
}
