using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class CreateCustomerRequestDTO
    {

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress]
        public string Email { get; set; }
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "FirstName là bắt buộc")]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "LastName là bắt buộc")]
        [MaxLength(100)]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [MaxLength(100)]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "giới tính là bắt buộc")]
        public Gender Gender { get; set; }
        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        public string Address { get; set; } = string.Empty; 

        public string? imgURL { get; set; } = string.Empty;


    }
}
