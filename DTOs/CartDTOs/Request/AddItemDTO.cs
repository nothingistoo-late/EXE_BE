using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CartDTOs.Request
{
    public class AddItemDto
    {
        public Guid BoxTypeId { get; set; }
        public int Quantity { get; set; }
    }

}
