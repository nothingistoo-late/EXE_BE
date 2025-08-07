using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.Customer.Responds
{
    public class CustomerRespondDTO
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Gender { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? ImgURL { get; set; } = string.Empty;

    }
}
