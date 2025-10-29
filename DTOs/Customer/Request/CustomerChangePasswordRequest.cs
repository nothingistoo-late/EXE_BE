using System.ComponentModel.DataAnnotations;

namespace DTOs.Customer.Request
{
    public class CustomerChangePasswordRequest
    {
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Mật khẩu cũ không được để trống")]
        public string OldPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Xác nhận mật khẩu mới không được để trống")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

