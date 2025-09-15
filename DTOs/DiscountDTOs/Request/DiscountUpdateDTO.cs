using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.DiscountDTOs.Request
{
    public class DiscountUpdateDTO
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
        public double? DiscountValue { get; set; }
        public bool? IsPercentage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
    }
}
