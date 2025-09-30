using System;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IOTPService
    {
        /// <summary>
        /// Tạo mã OTP mới cho user
        /// </summary>
        /// <param name="email">Email của user</param>
        /// <param name="purpose">Mục đích sử dụng OTP (ForgotPassword, EmailVerification, etc.)</param>
        /// <returns>Mã OTP được tạo</returns>
        Task<string> GenerateOTPAsync(string email, string purpose = "ForgotPassword");

        /// <summary>
        /// Xác thực mã OTP (không đánh dấu đã sử dụng)
        /// </summary>
        /// <param name="email">Email của user</param>
        /// <param name="otpCode">Mã OTP cần xác thực</param>
        /// <param name="purpose">Mục đích sử dụng OTP</param>
        /// <returns>True nếu OTP hợp lệ, False nếu không</returns>
        Task<bool> VerifyOTPAsync(string email, string otpCode, string purpose = "ForgotPassword");

        /// <summary>
        /// Xác thực mã OTP và đánh dấu đã sử dụng
        /// </summary>
        /// <param name="email">Email của user</param>
        /// <param name="otpCode">Mã OTP cần xác thực</param>
        /// <param name="purpose">Mục đích sử dụng OTP</param>
        /// <returns>True nếu OTP hợp lệ, False nếu không</returns>
        Task<bool> VerifyAndMarkOTPAsUsedAsync(string email, string otpCode, string purpose = "ForgotPassword");

        /// <summary>
        /// Xóa mã OTP sau khi sử dụng
        /// </summary>
        /// <param name="email">Email của user</param>
        /// <param name="purpose">Mục đích sử dụng OTP</param>
        Task RemoveOTPAsync(string email, string purpose = "ForgotPassword");

        /// <summary>
        /// Kiểm tra xem OTP có còn hiệu lực không
        /// </summary>
        /// <param name="email">Email của user</param>
        /// <param name="purpose">Mục đích sử dụng OTP</param>
        /// <returns>True nếu OTP còn hiệu lực, False nếu đã hết hạn</returns>
        Task<bool> IsOTPValidAsync(string email, string purpose = "ForgotPassword");

        /// <summary>
        /// Lấy thời gian còn lại của OTP (tính bằng phút)
        /// </summary>
        /// <param name="email">Email của user</param>
        /// <param name="purpose">Mục đích sử dụng OTP</param>
        /// <returns>Số phút còn lại, -1 nếu không tồn tại</returns>
        Task<int> GetOTPRemainingMinutesAsync(string email, string purpose = "ForgotPassword");
    }
}
