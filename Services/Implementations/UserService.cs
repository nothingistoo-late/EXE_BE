using DTOs.UserDTOs.Request;
using DTOs.UserDTOs.Identities;
using DTOs.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories;
using Repositories.Interfaces;
using Services.Helpers;
using Services.Helpers.Mapers;
using Services.Interfaces;
using Services.Interfaces.Services.Commons.User;
using System.Text;
using System.Text.Json;
using UserDTOs.DTOs.Response;
using Google.Apis.Auth;
using BusinessObjects;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;
        private readonly IUserEmailService _userEmailService;
        private readonly string _confirmEmailUri;
        private readonly string _resetPasswordUri;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentTime _currentTime;
        private readonly EXE_BE _context;
        private readonly GoogleOptions _googleOptions;
        private readonly IHttpClientFactory _httpClientFactory;

        public UserService(
            UserManager<User> userManager,
            ITokenService tokenService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<UserService> logger,
            IUserEmailService userEmailService,
            IConfiguration configuration,
            IUserRepository userRepository,
            ICurrentTime currentTime,
            EXE_BE context,
            IOptions<GoogleOptions> googleOptions,
            IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userEmailService = userEmailService ?? throw new ArgumentNullException(nameof(userEmailService));
            //_resetPasswordUri = configuration["Frontend:ResetPasswordUri"] ?? throw new ArgumentNullException(nameof(configuration), "ResetPasswordUri is missing");
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _currentTime = currentTime ?? throw new ArgumentNullException(nameof(currentTime));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _googleOptions = googleOptions?.Value ?? throw new ArgumentNullException(nameof(googleOptions));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        //public async Task<ApiResult<UserResponse>> RegisterAsync(UserRegisterRequest req)
        //{
        //    if (req == null || string.IsNullOrWhiteSpace(req.Email))
        //        return ApiResult<UserResponse>.Failure(new ArgumentException("Invalid request"));

        //    if (await _userRepository.ExistsByEmailAsync(req.Email))
        //        return ApiResult<UserResponse>.Failure(new InvalidOperationException("Email already in use"));

        //    var result = await _unitOfWork.ExecuteTransactionAsync(async () =>
        //    {
        //        var user = UserMappings.ToDomainUser(req);
        //        var createRes = await _userManager.CreateUserAsync(user, req.Password);
        //        if (!createRes.Succeeded)
        //            return ApiResult<UserResponse>.Failure(new InvalidOperationException(createRes.ErrorMessage));

        //        await _userManager.AddDefaultRoleAsync(user);
        //        return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(user, _userManager), "User registered successfully");
        //    });

        //    if (result.IsSuccess)
        //    {
        //        try
        //        {
        //            await SendWelcomeEmailsAsync(req.Email);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Failed to send welcome emails for {Email}", req.Email);
        //        }
        //    }
        //    return result;
        //}

        public async Task SendWelcomeEmailsAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return;

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Không cần encode token ở đây, để UserEmailService xử lý
            await Task.WhenAll(
                _userEmailService.SendWelcomeEmailAsync(email),
                _userEmailService.SendEmailConfirmationAsync(email, user.Id, token, _confirmEmailUri)
            );
        }

        public async Task<ApiResult<string>> ConfirmEmailAsync(Guid userId, string encodedToken)
        {
            if (string.IsNullOrWhiteSpace(encodedToken))
                return ApiResult<string>.Failure(new ArgumentException("Mã token không hợp lệ"));

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ApiResult<string>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode token for user {UserId}", userId);
                return ApiResult<string>.Failure(new ArgumentException("Định dạng token không hợp lệ"));
            }

            var res = await _userManager.ConfirmEmailAsync(user, token);
            if (!res.Succeeded)
            {
                _logger.LogWarning("Email confirmation failed for user {UserId}. Errors: {Errors}",
                    userId, string.Join(", ", res.Errors.Select(e => e.Description)));
                return ApiResult<string>.Failure(new InvalidOperationException("Xác minh email thất bại: " + string.Join(", ", res.Errors.Select(e => e.Description))));
            }

            return ApiResult<string>.Success("Email confirmed successfully", "Email confirmed successfully");
        }

        public async Task<ApiResult<string>> ResendConfirmationEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ApiResult<string>.Failure(new ArgumentException("Email không hợp lệ"));

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return ApiResult<string>.Success("If the email is valid and unconfirmed, a confirmation email will be sent", "Confirmation email process completed");

            if (await _userManager.IsEmailConfirmedAsync(user))
                return ApiResult<string>.Success("Email is already confirmed", "Email confirmation status checked");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Truyền token nguyên bản, để UserEmailService xử lý việc encode
            await _userEmailService.SendEmailConfirmationAsync(email, user.Id, token, _confirmEmailUri);
            return ApiResult<string>.Success("Confirmation email resent", "Confirmation email sent successfully");
        }

        public async Task<ApiResult<string>> InitiatePasswordResetAsync(ForgotPasswordRequestDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return ApiResult<string>.Failure(new ArgumentException("Email không hợp lệ"));

            var genericResponse = ApiResult<string>.Success("If the email is valid, you'll receive password reset instructions", "Password reset process initiated");
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                return genericResponse;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            _logger.LogDebug("Raw token: {Token}", token);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            _logger.LogDebug("Encoded token: {encodedToken}", encodedToken);

            await _userEmailService.SendPasswordResetEmailAsync(request.Email, encodedToken, _resetPasswordUri);
            return genericResponse;
        }

        public async Task<ApiResult<string>> ResetPasswordAsync(ResetPasswordRequestDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OTPCode) || string.IsNullOrWhiteSpace(request.NewPassword))
                return ApiResult<string>.Failure(new ArgumentException("Yêu cầu không hợp lệ"));

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return ApiResult<string>.Failure(new InvalidOperationException("Yêu cầu không hợp lệ"));
            _logger.LogDebug("Incoming OTP (request): {RequestOTP}", request.OTPCode);

            // Note: This method is deprecated in favor of the new OTP-based reset in AuthController
            // Keeping for backward compatibility but should use AuthController.ResetPassword instead
            return ApiResult<string>.Failure(new InvalidOperationException("Phương thức này đã bị ngừng. Vui lòng sử dụng /api/Auth/reset-password."));
        }

        //public async Task<ApiResult<PagedList<UserDetailsDTO>>> SearchUsersAsync(
        //string? searchTerm,
        //RoleType? roleId,
        //int page,
        //int size)
        //{
        //    if (page < 1 || size < 1)
        //        return ApiResult<PagedList<UserDetailsDTO>>.Failure(
        //            new ArgumentException("Tham số phân trang không hợp lệ"));

        //    var list = await _userRepository.SearchUsersAsync(searchTerm, roleId, page, size);

        //    var dtoList = list.Select(u => UserMappings.ToUserDetailsDTO(u, _userManager)).ToList();

        //    return ApiResult<PagedList<UserDetailsDTO>>.Success(
        //        new PagedList<UserDetailsDTO>(dtoList, list.MetaData.TotalCount, page, size),
        //        $"Tìm thấy {list.MetaData.TotalCount} kết quả");
        //}
        public async Task<ApiResult<string>> Send2FACodeAsync()
        {
            var userId = _currentUserService.GetUserId();
            if (userId == null)
                return ApiResult<string>.Failure(new InvalidOperationException("User not found"));

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ApiResult<string>.Failure(new InvalidOperationException("User not found"));

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            await _userEmailService.Send2FACodeAsync(user.Email, code);
            return ApiResult<string>.Success("2FA code sent", "Two-factor authentication code sent successfully");
        }

        //public async Task<ApiResult<UserResponse>> AdminRegisterAsync(AdminCreateUserRequest req)
        //{
        //    if (req == null || string.IsNullOrWhiteSpace(req.Email))
        //        return ApiResult<UserResponse>.Failure(new ArgumentException("Invalid request"));

        //    if (!_currentUserService.IsAdmin())
        //        return ApiResult<UserResponse>.Failure(new UnauthorizedAccessException("Forbidden: Only admins can register users"));

        //    if (await _userRepository.ExistsByEmailAsync(req.Email))
        //        return ApiResult<UserResponse>.Failure(new InvalidOperationException("Email already in use"));

        //    _logger.LogInformation("Admin registering user: {Email}", req.Email);

        //    return await _unitOfWork.ExecuteTransactionAsync(async () =>
        //    {
        //        var user = UserMappings.ToDomainUser(req);
        //        var create = await _userManager.CreateUserAsync(user, req.Password);
        //        if (!create.Succeeded)
        //            return ApiResult<UserResponse>.Failure(new InvalidOperationException(create.ErrorMessage));

        //        await _userManager.AddRolesAsync(user, req.Roles);
        //        var token = await _tokenService.GenerateToken(user);
        //        return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(user, _userManager, token.Data), "User created successfully by admin");
        //    });
        //}

        public async Task<ApiResult<UserResponse>> LoginAsync(UserLoginRequest req)
        {
            // 1. Guard clause
            if (req is null || string.IsNullOrWhiteSpace(req.EmailOrPhoneNumber) || string.IsNullOrWhiteSpace(req.Password))
                return ApiResult<UserResponse>.Failure(new ArgumentException("Yêu cầu không hợp lệ"));

            _logger.LogInformation("Login attempt: {Login}", req.EmailOrPhoneNumber);

            // 2. Locate user (email OR username)
            var user = req.EmailOrPhoneNumber.Contains('@', StringComparison.OrdinalIgnoreCase)
                ? await _userManager.FindByEmailAsync(req.EmailOrPhoneNumber)
                : await _unitOfWork.UserRepository.FirstOrDefaultAsync(c=> c.PhoneNumber == req.EmailOrPhoneNumber);

            // 3. Validate credentials
            if (user is null || !await _userManager.CheckPasswordAsync(user, req.Password))
            {
                if (user is not null)
                    await _userManager.AccessFailedAsync(user);
                return ApiResult<UserResponse>.Failure(new UnauthorizedAccessException("Tên đăng nhập/email hoặc mật khẩu không đúng"));
            }

            // 4. Verify account state
            //if (!await _userManager.IsEmailConfirmedAsync(user))
            //    return ApiResult<UserResponse>.Failure(new InvalidOperationException("Please confirm your email before logging in"));

            if (await _userManager.IsLockedOutAsync(user))
                return ApiResult<UserResponse>.Failure(new InvalidOperationException("Tài khoản đã bị khóa"));

            // 5. Reset failed-count & issue tokens
            await _userManager.ResetAccessFailedAsync(user);

            var tokenResult = await _tokenService.GenerateToken(user);
            var refreshTokenInfo = _tokenService.GenerateRefreshToken();
            await _userManager.SetRefreshTokenAsync(user, refreshTokenInfo);

            var userResponse = await UserMappings.ToUserResponseAsync(
                user, _userManager, tokenResult.Data, refreshTokenInfo.Token);

            return ApiResult<UserResponse>.Success(userResponse, "Login successful");
        }

        public async Task<ApiResult<UserResponse>> GoogleLoginAsync(GoogleLoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.TokenId))
                {
                    return ApiResult<UserResponse>.Failure(new ArgumentException("Token không hợp lệ"));
                }

                _logger.LogInformation("Google login attempt with token");

                GoogleJsonWebSignature.Payload payload = null;
                string email = null;
                string firstName = null;
                string lastName = null;

                // Kiểm tra xem đây là ID Token hay Access Token
                if (request.TokenId.StartsWith("ya29.") || request.TokenId.StartsWith("1//"))
                {
                    // Đây là Access Token - gọi Google UserInfo API
                    _logger.LogInformation("Detected Google Access Token, fetching user info");
                    try
                    {
                        var httpClient = _httpClientFactory.CreateClient();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.TokenId);
                        
                        var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                        if (!response.IsSuccessStatusCode)
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogWarning("Failed to get user info from Google: {StatusCode} - {Content}", response.StatusCode, errorContent);
                            return ApiResult<UserResponse>.Failure(new UnauthorizedAccessException("Không thể lấy thông tin từ Google token"));
                        }

                        var jsonContent = await response.Content.ReadAsStringAsync();
                        var userInfo = JsonSerializer.Deserialize<GoogleUserInfoResponse>(jsonContent, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });

                        if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                        {
                            return ApiResult<UserResponse>.Failure(new InvalidOperationException("Không thể lấy email từ Google token"));
                        }

                        email = userInfo.Email;
                        firstName = userInfo.GivenName;
                        lastName = userInfo.FamilyName;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching user info from Google");
                        return ApiResult<UserResponse>.Failure(new Exception($"Lỗi khi lấy thông tin từ Google: {ex.Message}"));
                    }
                }
                else
                {
                    // Đây là ID Token - verify như bình thường
                    _logger.LogInformation("Detected Google ID Token, validating");
                    try
                    {
                        var settings = new GoogleJsonWebSignature.ValidationSettings
                        {
                            Audience = new[] { _googleOptions.ClientId }
                        };

                        payload = await GoogleJsonWebSignature.ValidateAsync(request.TokenId, settings);
                        
                        if (string.IsNullOrEmpty(payload.Email))
                        {
                            return ApiResult<UserResponse>.Failure(new InvalidOperationException("Không thể lấy email từ Google token"));
                        }

                        email = payload.Email;
                        firstName = payload.GivenName;
                        lastName = payload.FamilyName;
                    }
                    catch (InvalidJwtException ex)
                    {
                        _logger.LogWarning(ex, "Invalid Google ID token");
                        return ApiResult<UserResponse>.Failure(new UnauthorizedAccessException("Token Google không hợp lệ hoặc đã hết hạn"));
                    }
                }

                // Tìm hoặc tạo user
                var user = await _userManager.FindByEmailAsync(email);
                var isNewUser = user == null;

                if (isNewUser)
                {
                    // Tạo user mới
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true, // Google email đã được xác thực
                        FirstName = firstName ?? string.Empty,
                        LastName = lastName ?? string.Empty,
                        CreatedAt = _currentTime.GetVietnamTime(),
                        CreatedBy = Guid.Empty,
                        UpdatedAt = _currentTime.GetVietnamTime(),
                        UpdatedBy = Guid.Empty,
                        IsDeleted = false
                    };

                    // Tạo user với password ngẫu nhiên (sẽ không bao giờ dùng)
                    var randomPassword = Guid.NewGuid().ToString() + "A1!";
                    var createResult = await _userManager.CreateAsync(user, randomPassword);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create Google user: {Errors}", errors);
                        return ApiResult<UserResponse>.Failure(new InvalidOperationException($"Không thể tạo tài khoản: {errors}"));
                    }

                    // Thêm role USER
                    await _userManager.AddToRoleAsync(user, "USER");

                    // Tạo Customer record
                    var customer = new Customer
                    {
                        UserId = user.Id,
                        Address = string.Empty,
                        CreatedAt = _currentTime.GetVietnamTime(),
                        CreatedBy = user.Id,
                        UpdatedAt = _currentTime.GetVietnamTime(),
                        UpdatedBy = user.Id,
                        IsDeleted = false
                    };
                    await _unitOfWork.CustomerRepository.AddAsync(customer);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Created new Google user: {Email}", email);
                }
                else
                {
                    // Update thông tin từ Google nếu cần
                    if (string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(firstName))
                    {
                        user.FirstName = firstName;
                    }
                    if (string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(lastName))
                    {
                        user.LastName = lastName;
                    }
                    user.UpdatedAt = _currentTime.GetVietnamTime();
                    await _userManager.UpdateAsync(user);
                }

                // Kiểm tra account bị khóa
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return ApiResult<UserResponse>.Failure(new InvalidOperationException("Tài khoản đã bị khóa"));
                }

                // Generate tokens
                var tokenResult = await _tokenService.GenerateToken(user);
                var refreshTokenInfo = _tokenService.GenerateRefreshToken();
                await _userManager.SetRefreshTokenAsync(user, refreshTokenInfo);

                var userResponse = await UserMappings.ToUserResponseAsync(
                    user, _userManager, tokenResult.Data, refreshTokenInfo.Token);

                var message = isNewUser ? "Đăng ký và đăng nhập thành công với Google" : "Đăng nhập thành công với Google";
                return ApiResult<UserResponse>.Success(userResponse, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
                return ApiResult<UserResponse>.Failure(new Exception($"Lỗi khi đăng nhập Google: {ex.Message}"));
            }
        }

        private class GoogleUserInfoResponse
        {
            public string Email { get; set; } = string.Empty;
            public string GivenName { get; set; } = string.Empty;
            public string FamilyName { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        public async Task<ApiResult<UserResponse>> GetByIdAsync(Guid id)
        {
            var userDetails = await _userRepository.GetUserDetailsByIdAsync(id);
            if (userDetails == null)
                return ApiResult<UserResponse>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(userDetails, _userManager), "User retrieved successfully");
        }

        public async Task<ApiResult<CurrentUserResponse>> GetCurrentUserAsync()
        {
            var uid = _currentUserService.GetUserId();
            if (uid == null)
                return ApiResult<CurrentUserResponse>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user == null)
                return ApiResult<CurrentUserResponse>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            var token = await _tokenService.GenerateToken(user);
            var resp = UserMappings.ToCurrentUserResponse(user, token.Data);
            // Enrich with customer profile (address)
            try
            {
                var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(user.Id);
                if (customer != null && !string.IsNullOrWhiteSpace(customer.Address))
                {
                    resp.Address = customer.Address;
                }
            }
            catch { }
            return ApiResult<CurrentUserResponse>.Success(resp, "Current user retrieved successfully");
        }

        public async Task<ApiResult<CurrentUserResponse>> RefreshTokenAsync(RefreshTokenRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RefreshToken))
                return ApiResult<CurrentUserResponse>.Failure(new ArgumentException("Yêu cầu không hợp lệ"));

            // Tìm user từ refresh token trong database (không cần access token vì nó có thể đã expired)
            User? user = null;
            RefreshTokenInfo? matchedTokenInfo = null;
            
            try
            {
                var currentTime = _currentTime.GetVietnamTime();
                var refreshToken = req.RefreshToken.Trim();

                // Query tất cả refresh tokens và deserialize để tìm match
                var userTokens = await _context.UserTokens
                    .Where(ut => 
                        ut.LoginProvider == "System Admin" && 
                        ut.Name == "RefreshToken" &&
                        !string.IsNullOrEmpty(ut.Value))
                    .ToListAsync();

                _logger.LogInformation("Checking {Count} refresh tokens", userTokens.Count);

                foreach (var userToken in userTokens)
                {
                    try
                    {
                        var tokenInfo = JsonSerializer.Deserialize<RefreshTokenInfo>(userToken.Value);
                        if (tokenInfo == null)
                            continue;

                        // So sánh token (case-sensitive, exact match)
                        if (tokenInfo.Token?.Trim() == refreshToken)
                        {
                            // Kiểm tra expiry
                            if (tokenInfo.Expiry > currentTime)
                            {
                                // Tìm thấy token hợp lệ, lấy user
                                user = await _userManager.FindByIdAsync(userToken.UserId.ToString());
                                if (user != null)
                                {
                                    matchedTokenInfo = tokenInfo;
                                    _logger.LogInformation("Found valid refresh token for user {UserId}", user.Id);
                                    break;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Refresh token found but expired. Expiry: {Expiry}, Current: {Current}", 
                                    tokenInfo.Expiry, currentTime);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize refresh token for user {UserId}", userToken.UserId);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing refresh token for user {UserId}", userToken.UserId);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding user by refresh token");
            }

            // Nếu không tìm thấy user hoặc token không hợp lệ
            if (user == null || matchedTokenInfo == null)
            {
                _logger.LogWarning("Invalid or expired refresh token");
                return ApiResult<CurrentUserResponse>.Failure(new UnauthorizedAccessException("Refresh token không hợp lệ hoặc đã hết hạn"));
            }

            // Generate new token pair
            var token = await _tokenService.GenerateToken(user);
            var newRefreshTokenInfo = _tokenService.GenerateRefreshToken();
            await _userManager.SetRefreshTokenAsync(user, newRefreshTokenInfo);
            
            var response = UserMappings.ToCurrentUserResponse(user, token.Data, newRefreshTokenInfo.Token);
            
            return ApiResult<CurrentUserResponse>.Success(response, "Token refreshed successfully");
        }

        public async Task<ApiResult<RevokeRefreshTokenResponse>> RevokeRefreshTokenAsync(RefreshTokenRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RefreshToken))
                return ApiResult<RevokeRefreshTokenResponse>.Failure(new ArgumentException("Yêu cầu không hợp lệ"));

            var uid = _currentUserService.GetUserId();
            if (uid == null)
                return ApiResult<RevokeRefreshTokenResponse>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user == null || !await _userManager.ValidateRefreshTokenAsync(user, req.RefreshToken))
                return ApiResult<RevokeRefreshTokenResponse>.Failure(new UnauthorizedAccessException("Refresh token không hợp lệ"));

            var rem = await _userManager.RemoveRefreshTokenAsync(user);
            if (!rem.Succeeded)
                return ApiResult<RevokeRefreshTokenResponse>.Failure(new InvalidOperationException(rem.ErrorMessage));

            return ApiResult<RevokeRefreshTokenResponse>.Success(new RevokeRefreshTokenResponse { Message = "Revoked" }, "Refresh token revoked successfully");
        }

        public async Task<ApiResult<string>> ChangePasswordAsync(ChangePasswordRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.OldPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                return ApiResult<string>.Failure(new ArgumentException("Yêu cầu không hợp lệ"));

            var uid = _currentUserService.GetUserId();
            if (uid == null)
                return ApiResult<string>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user == null)
                return ApiResult<string>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            var res = await _userManager.ChangeUserPasswordAsync(user, req.OldPassword, req.NewPassword);
            if (!res.Succeeded)
                return ApiResult<string>.Failure(new InvalidOperationException(res.ErrorMessage));

            await _userManager.UpdateSecurityStampAsync(user);
            await _userEmailService.SendPasswordChangedNotificationAsync(user.Email);
            return ApiResult<string>.Success("Password changed successfully", "Password changed successfully");
        }

        public async Task<ApiResult<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest req)
        {
            if (req == null)
                return ApiResult<UserResponse>.Failure(new ArgumentException("Yêu cầu không hợp lệ"));

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return ApiResult<UserResponse>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                // 1. Cập nhật thông tin user
                UserMappings.ApplyUpdate(req, user);
                user.UpdatedAt = _currentTime.GetVietnamTime();

                // 2. Cập nhật role
                if (req.Roles?.Any() == true && _currentUserService.IsAdmin())
                {
                    await _userRepository.UpdateRolesAsync(user, req.Roles);
                }

                // 3. Lưu toàn bộ thay đổi
                await _unitOfWork.SaveChangesAsync();

                return ApiResult<UserResponse>.Success(
                    await UserMappings.ToUserResponseAsync(user, _userManager),
                    "User updated successfully");
            });
        }

        public async Task<ApiResult<UserResponse>> UpdateCurrentUserAsync(UpdateUserRequest req)
        {
            if (req == null)
                return ApiResult<UserResponse>.Failure(new ArgumentException("Yêu cầu không hợp lệ"));

            var uid = _currentUserService.GetUserId();
            if (uid == null)
                return ApiResult<UserResponse>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user == null)
                return ApiResult<UserResponse>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                UserMappings.ApplyUpdate(req, user);
                user.UpdatedAt = _currentTime.GetVietnamTime();

                var upd = await _userManager.UpdateAsync(user);
                if (!upd.Succeeded)
                    return ApiResult<UserResponse>.Failure(new InvalidOperationException(string.Join(", ", upd.Errors.Select(e => e.Description))));

                return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(user, _userManager), "Current user updated successfully");
            });
        }

        public Task<ApiResult<UserResponse>> LockUserAsync(Guid id) =>
            ChangeLockoutAsync(id, true, DateTimeOffset.MaxValue);

        public Task<ApiResult<UserResponse>> UnlockUserAsync(Guid id) =>
            ChangeLockoutAsync(id, false, DateTimeOffset.UtcNow);

        private async Task<ApiResult<UserResponse>> ChangeLockoutAsync(Guid id, bool enable, DateTimeOffset until)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return ApiResult<UserResponse>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

            var res = await _userManager.SetLockoutAsync(user, enable, until);
            if (!res.Succeeded)
                return ApiResult<UserResponse>.Failure(new InvalidOperationException(res.ErrorMessage));

            string message = enable ? "Khóa tài khoản thành công" : "Mở khóa tài khoản thành công";
            return ApiResult<UserResponse>.Success(await UserMappings.ToUserResponseAsync(user, _userManager), message);
        }

        public async Task<ApiResult<object>> DeleteUsersAsync(List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                return ApiResult<object>.Success(null, "Không có người dùng để xóa");

            // Lấy user đang đăng nhập
            var currentUserId = _currentUserService.GetUserId();
            if (currentUserId.HasValue && ids.Contains(currentUserId.Value))
                return ApiResult<object>.Failure(new InvalidOperationException("Không thể xóa chính mình khi đang đăng nhập"));

            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                foreach (var id in ids)
                {
                    var user = await _userManager.FindByIdAsync(id.ToString());
                    if (user == null)
                        return ApiResult<object>.Failure(new InvalidOperationException($"Không tìm thấy người dùng {id}"));

                    var del = await _userManager.DeleteAsync(user);
                    if (!del.Succeeded)
                        return ApiResult<object>.Failure(new InvalidOperationException(string.Join(", ", del.Errors.Select(e => e.Description))));
                }
                return ApiResult<object>.Success(null, $"Đã xóa thành công {ids.Count} người dùng");
            });
        }
        public async Task<ApiResult<PagedList<UserDetailsDTO>>> GetUsersAsync(int page, int size)
        {
            if (page < 1 || size < 1)
                return ApiResult<PagedList<UserDetailsDTO>>.Failure(new ArgumentException("Tham số phân trang không hợp lệ"));

            var list = await _userRepository.GetUserDetailsAsync(page, size);
            return ApiResult<PagedList<UserDetailsDTO>>.Success(list, $"Lấy {list.Count} người dùng từ trang {page}");
        }

    }
}