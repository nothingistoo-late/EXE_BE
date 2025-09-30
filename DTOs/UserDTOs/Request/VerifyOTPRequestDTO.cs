using System.ComponentModel.DataAnnotations;

namespace DTOs.UserDTOs.Request
{
    public class VerifyOTPRequestDTO
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP code must contain only digits")]
        public string OTPCode { get; set; } = string.Empty;
    }
}
