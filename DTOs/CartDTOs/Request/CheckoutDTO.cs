using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CartDTOs.Request
{
    public class CheckoutDto
    {
        public PaymentMethod PaymentMethod { get; set; }
        public DeliveryMethod DeliveryMethod { get; set; }
        public string? DiscountCode { get; set; }
        
        // Required delivery information
        [Required]
        [MaxLength(1000)]
        public string Address { get; set; } = null!;
        
        [Required]
        [MaxLength(100)]
        public string DeliveryTo { get; set; } = null!;
        
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = null!;
    }
}
