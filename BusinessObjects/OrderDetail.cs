using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class OrderDetail : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid BoxTypeId { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public Order? Order { get; set; }
        public BoxTypes? BoxType { get; set; }
    }
}
