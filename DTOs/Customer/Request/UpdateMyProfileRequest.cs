using BusinessObjects;
using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.Customer.Request
{
    public class UpdateMyProfileRequest
    {
        // Thuộc User
        public string? FirstName { get; set; } = null!;
        public string? LastName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public Gender? Gender { get; set; } = null!;

        // Thuộc Customer
        public string? Address { get; set; } = null!;
        public string? ImgURL { get; set; }
    }
}
