using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.OrderDTOs.Respond
{
    public class UpdateOrderStatusResult
    {
        public Guid OrderId { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }

}
