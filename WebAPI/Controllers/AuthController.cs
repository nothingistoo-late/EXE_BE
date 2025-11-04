using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services.Interfaces;
using DTOs.UserDTOs.Request;
using DTOs.UserDTOs.Response;
using BusinessObjects;
using Microsoft.AspNetCore.Identity;
using Services.Commons.Gmail;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IOTPService _otpService;
        private readonly IEXEGmailService _emailService;
        private readonly UserManager<User> _userManager;

        public AuthController(
            IUserService userService,
            IOTPService otpService,
            IEXEGmailService emailService,
            UserManager<User> userManager)
        {
            _userService = userService;
            _otpService = otpService;
            _emailService = emailService;
            _userManager = userManager;
        }

        // ========== CÁC HÀM AUTH GỐC ==========

        /// <summary>
        /// Đăng nhập
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var result = await _userService.LoginAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Đăng nhập bằng Google
        /// </summary>
        [HttpPost("login/google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var result = await _userService.GoogleLoginAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Đăng ký tài khoản (tạm thời không khả dụng)
        /// </summary>
        //[HttpPost("register")]
        //public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        //{
        //    return BadRequest(new { Message = "Đăng ký tài khoản tạm thời không khả dụng. Vui lòng liên hệ admin." });
        //}

        /// <summary>
        /// Làm mới token
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _userService.RefreshTokenAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Thu hồi refresh token (logout)
        /// </summary>
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _userService.RevokeRefreshTokenAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin user hiện tại
        /// </summary>
        [HttpGet("current-user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var result = await _userService.GetCurrentUserAsync();
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Kiểm tra thời gian sống của token hiện tại
        /// </summary>
        [AllowAnonymous]
        [HttpGet("check-token")]
        [Authorize]
        public IActionResult CheckToken()
        {
            try
            {
                // Lấy token từ header Authorization
                var authHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return BadRequest(new
                    {
                        isSuccess = false,
                        message = "Token không tìm thấy trong header"
                    });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // Decode token (không validate, chỉ lấy thông tin)
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                
                // Lấy thời gian hết hạn (ValidTo hoặc từ Exp claim)
                var expiryTime = jsonToken.ValidTo;
                
                if (expiryTime == DateTime.MinValue)
                {
                    // Fallback: lấy từ Expiration claim nếu ValidTo không có
                    var expValue = jsonToken.Payload.Expiration;
                    if (!expValue.HasValue)
                    {
                        return BadRequest(new
                        {
                            isSuccess = false,
                            message = "Token không có thông tin hết hạn"
                        });
                    }
                    expiryTime = DateTimeOffset.FromUnixTimeSeconds(expValue.Value).DateTime;
                }
                var currentTime = DateTime.UtcNow;
                var remainingTime = expiryTime - currentTime;
                var isExpired = remainingTime <= TimeSpan.Zero;

                return Ok(new
                {
                    isSuccess = true,
                    message = isExpired ? "Token đã hết hạn" : "Token còn hiệu lực",
                    data = new
                    {
                        expiresAt = expiryTime,
                        expiresAtLocal = expiryTime.ToLocalTime(),
                        remainingMinutes = isExpired ? 0 : Math.Max(0, (int)remainingTime.TotalMinutes),
                        remainingSeconds = isExpired ? 0 : Math.Max(0, (int)remainingTime.TotalSeconds),
                        isExpired = isExpired,
                        currentTime = currentTime,
                        currentTimeLocal = currentTime.ToLocalTime()
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    message = $"Lỗi khi kiểm tra token: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Thay đổi mật khẩu
        /// </summary>
        //[HttpPost("change-password")]
        //public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        //{
        //    var result = await _userService.ChangePasswordAsync(request);
        //    if (!result.IsSuccess)
        //    {
        //        return BadRequest(result);
        //    }
        //    return Ok(result);
        //}

        // ========== CÁC HÀM OTP MỚI ==========

        /// <summary>
        /// Gửi mã OTP để đặt lại mật khẩu
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ForgotPasswordResponseDTO>> ForgotPassword([FromBody] ForgotPasswordRequestDTO request)
        {
            try
            {
                // Kiểm tra user có tồn tại không
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Ok(new ForgotPasswordResponseDTO
                    {
                        Success = false,
                        Message = "Email không tồn tại trong hệ thống."
                    });
                }

                // Tạo mã OTP
                Console.WriteLine($"DEBUG: AuthController - Generating OTP for email: {request.Email}");
                var otpCode = await _otpService.GenerateOTPAsync(request.Email, "ForgotPassword");
                Console.WriteLine($"DEBUG: AuthController - Generated OTP: {otpCode}");
                var remainingMinutes = await _otpService.GetOTPRemainingMinutesAsync(request.Email, "ForgotPassword");
                Console.WriteLine($"DEBUG: AuthController - Remaining minutes: {remainingMinutes}");

                // Gửi email OTP
                await _emailService.SendForgotPasswordOTPEmailAsync(request.Email, user.UserName ?? user.Email, otpCode);

                return Ok(new ForgotPasswordResponseDTO
                {
                    Success = true,
                    Message = $"Mã OTP đã được gửi đến email {request.Email}. Mã có hiệu lực trong {remainingMinutes} phút.",
                    RemainingMinutes = remainingMinutes
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ForgotPasswordResponseDTO
                {
                    Success = false,
                    Message = $"Lỗi khi gửi mã OTP: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Đặt lại mật khẩu với mã OTP
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ResetPasswordResponseDTO>> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            try
            {
                // Kiểm tra user có tồn tại không
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Ok(new ResetPasswordResponseDTO
                    {
                        Success = false,
                        Message = "Email không tồn tại trong hệ thống."
                    });
                }

                // Xác thực OTP và đánh dấu đã sử dụng
                Console.WriteLine($"DEBUG: AuthController - Verifying OTP for email: {request.Email}, OTP: {request.OTPCode}");
                var isValidOTP = await _otpService.VerifyAndMarkOTPAsUsedAsync(request.Email, request.OTPCode, "ForgotPassword");
                Console.WriteLine($"DEBUG: AuthController - OTP verification result: {isValidOTP}");
                if (!isValidOTP)
                {
                    return Ok(new ResetPasswordResponseDTO
                    {
                        Success = false,
                        Message = "Mã OTP không hợp lệ hoặc đã hết hạn."
                    });
                }

                // Đặt lại mật khẩu
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

                if (result.Succeeded)
                {
                    // Xóa OTP sau khi sử dụng thành công
                    await _otpService.RemoveOTPAsync(request.Email, "ForgotPassword");

                    // Gửi email thông báo thay đổi mật khẩu thành công
                    await _emailService.SendPasswordChangedEmailAsync(request.Email, user.UserName ?? user.Email);

                    return Ok(new ResetPasswordResponseDTO
                    {
                        Success = true,
                        Message = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập với mật khẩu mới."
                    });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Ok(new ResetPasswordResponseDTO
                    {
                        Success = false,
                        Message = $"Không thể đặt lại mật khẩu: {errors}"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ResetPasswordResponseDTO
                {
                    Success = false,
                    Message = $"Lỗi khi đặt lại mật khẩu: {ex.Message}"
                });
            }
        }
    }
}