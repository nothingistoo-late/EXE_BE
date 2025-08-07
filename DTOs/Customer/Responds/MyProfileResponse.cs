using BusinessObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.Customer.Responds
{
    public class MyProfileResponse
    {
        // User
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FullName => $"{LastName} {FirstName}".Trim();
        public string Email { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string Phone { get; set; } = null!;
        // Customer
        public string Address { get; set; } = null!;
        public string? ImgURL { get; set; }
    }
}
