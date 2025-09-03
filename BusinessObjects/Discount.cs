using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Discount
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;   // mã giảm giá: "SALE20"
        public string Description { get; set; } = string.Empty;
        public double DiscountValue { get; set; }          // số tiền giảm hoặc %
        public bool IsPercentage { get; set; }             // true = %, false = số tiền
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
