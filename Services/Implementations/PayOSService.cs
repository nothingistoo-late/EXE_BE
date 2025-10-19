using DTOs.Options;
using DTOs.PayOSDTOs;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Commons;
using BusinessObjects.Common;
using System.Text.Json;

namespace Services.Implementations
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOSOptions _options;
        private readonly ILogger<PayOSService> _logger;
        private readonly HttpClient _httpClient;

        public PayOSService(IOptions<PayOSOptions> options, ILogger<PayOSService> logger, HttpClient httpClient)
        {
            _options = options.Value;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<ApiResult<PaymentLinkResponse>> CreatePaymentLinkAsync(CreatePaymentLinkRequest request)
        {
            try
            {
                // Tạo payment data theo PayOS API format
                // PayOS yêu cầu orderCode tối đa 12 chữ số
                var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000000000000; // 12 digits max
                
                // Đảm bảo amount > 0 (PayOS yêu cầu)
                var amount = Math.Max(1, (int)request.Amount);

                // URL-encode description để tránh ký tự đặc biệt và giới hạn 25 ký tự
                var safeDescription = (request.Description?.Replace("#", "") ?? "Payment for Order").Substring(0, Math.Min(25, (request.Description?.Replace("#", "") ?? "Payment for Order").Length));

                // Tạo signature trước khi tạo paymentData
                var signature = CreatePayOSSignature(amount.ToString(), orderCode.ToString(), safeDescription, _options.ReturnUrl, _options.CancelUrl, _options.ChecksumKey);

                var paymentData = new
                {
                    orderCode,
                    amount,
                    description = safeDescription,
                    items = request.Items.Select(item => new
                    {
                        name = !string.IsNullOrEmpty(item.Name) ? item.Name : "Product",
                        quantity = Math.Max(1, item.Quantity),
                        price = Math.Max(1, (int)item.Price)
                    }).ToList(),
                    cancelUrl = _options.CancelUrl,
                    returnUrl = _options.ReturnUrl,
                    expiredAt = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                    signature // PayOS v2 yêu cầu signature trong body JSON
                };

                var json = JsonSerializer.Serialize(paymentData);
                _logger.LogInformation("PayOS Request Data: {RequestData}", json);
                
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", _options.ClientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("x-idempotency-key", Guid.NewGuid().ToString());
                
                // Log signature data
                _logger.LogInformation("PayOS Signature Data: amount={Amount}, orderCode={OrderCode}, description={Description}, returnUrl={ReturnUrl}, cancelUrl={CancelUrl}", 
                    amount, orderCode, safeDescription, _options.ReturnUrl, _options.CancelUrl);
                _logger.LogInformation("PayOS Signature: {Signature}", signature);
                
                // PayOS v2 không dùng x-signature header nữa, signature nằm trong body
                
                _logger.LogInformation("PayOS Headers: x-client-id={ClientId}, x-api-key={ApiKey}", 
                    _options.ClientId, _options.ApiKey);
                
                var response = await _httpClient.PostAsync("https://api-merchant.payos.vn/v2/payment-requests", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("PayOS API Response - Status: {StatusCode}, Content: {ResponseContent}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        // Kiểm tra nếu có lỗi từ PayOS
                        if (responseJson.TryGetProperty("code", out var codeElement))
                        {
                            var code = codeElement.GetString();
                            if (code != "00") // PayOS success code
                            {
                                var desc = responseJson.TryGetProperty("desc", out var descElement) ? descElement.GetString() : "Unknown error";
                                _logger.LogError("PayOS API error: {Code} - {Description}", code, desc);
                                return ApiResult<PaymentLinkResponse>.Failure(new Exception($"PayOS API error: {desc}"));
                            }
                        }
                        
                        // Parse response data
                        if (responseJson.TryGetProperty("data", out var dataElement))
                        {
                            var result = new PaymentLinkResponse
                            {
                                PaymentLinkId = dataElement.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                                PaymentUrl = dataElement.TryGetProperty("checkoutUrl", out var url) ? url.GetString() ?? "" : "",
                                Amount = dataElement.TryGetProperty("amount", out var responseAmount) ? responseAmount.GetInt32() : request.Amount,
                                Description = dataElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : request.Description,
                                OrderCode = orderCode, // Trả về OrderCode đã tạo
                                Status = "ACTIVE"
                            };

                            _logger.LogInformation("Created PayOS payment link for order {OrderId}", request.OrderId);
                            return ApiResult<PaymentLinkResponse>.Success(result, "Payment link created successfully");
                        }
                        
                        _logger.LogWarning("PayOS API returned no data. Response: {ResponseContent}", responseContent);
                        return ApiResult<PaymentLinkResponse>.Failure(new Exception("PayOS API returned no data"));
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse PayOS response: {ResponseContent}", responseContent);
                        return ApiResult<PaymentLinkResponse>.Failure(new Exception($"Failed to parse PayOS response: {ex.Message}"));
                    }
                }
                else
                {
                    _logger.LogError("PayOS API error: {StatusCode} - {ResponseContent}", response.StatusCode, responseContent);
                    return ApiResult<PaymentLinkResponse>.Failure(new Exception($"PayOS API error: {response.StatusCode}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment link for order {OrderId}", request.OrderId);
                return ApiResult<PaymentLinkResponse>.Failure(new Exception(ex.Message));
            }
        }

        public async Task<ApiResult<object>> GetPaymentInformationAsync(string paymentLinkId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", _options.ClientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
                
                var response = await _httpClient.GetAsync($"https://api-merchant.payos.vn/v2/payment-requests/{paymentLinkId}");
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var paymentInfo = JsonSerializer.Deserialize<object>(responseContent);
                return ApiResult<object>.Success(paymentInfo, "Payment information retrieved successfully");
                }
                else
                {
                    return ApiResult<object>.Failure(new Exception($"PayOS API error: {response.StatusCode}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment information for payment link {PaymentLinkId}", paymentLinkId);
                return ApiResult<object>.Failure(new Exception(ex.Message));
            }
        }

        public async Task<ApiResult<bool>> CancelPaymentLinkAsync(string paymentLinkId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", _options.ClientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
                
                var response = await _httpClient.DeleteAsync($"https://api-merchant.payos.vn/v2/payment-requests/{paymentLinkId}");
                
                if (response.IsSuccessStatusCode)
                {
                return ApiResult<bool>.Success(true, "Payment link cancelled successfully");
                }
                else
                {
                    return ApiResult<bool>.Failure(new Exception($"PayOS API error: {response.StatusCode}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment link {PaymentLinkId}", paymentLinkId);
                return ApiResult<bool>.Failure(new Exception(ex.Message));
            }
        }

        public async Task<ApiResult<bool>> VerifyWebhookDataAsync(object webhookData)
        {
            try
            {
                // Implement webhook verification logic here
                return ApiResult<bool>.Success(true, "Webhook data is valid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying webhook data");
                return ApiResult<bool>.Failure(new Exception(ex.Message));
            }
        }

        private string CreatePayOSSignature(string amount, string orderCode, string description, string returnUrl, string cancelUrl, string checksumKey)
        {
            // PayOS v2 yêu cầu thứ tự: amount&cancelUrl&description&orderCode&returnUrl
            string data = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(checksumKey));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private string CreatePayOSSignatureV2(string amount, string orderCode, string description, string returnUrl, string cancelUrl, string checksumKey)
        {
            // Base64 version với format chuẩn PayOS v2
            string data = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(checksumKey));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashBytes);
    }

    public async Task<bool> VerifyWebhookDataAsync(PayOSWebhookData webhookData)
    {
        try
        {
            // PayOS webhook signature format: amount&cancelUrl&description&orderCode&returnUrl
            string data = $"{webhookData.Amount}&{_options.CancelUrl}&Payment&{webhookData.OrderCode}&{_options.ReturnUrl}";
            
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_options.ChecksumKey));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            var expectedSignature = Convert.ToHexString(hashBytes).ToLower();
            
            _logger.LogInformation("PayOS Webhook Verification - Data: {Data}", data);
            _logger.LogInformation("PayOS Webhook Verification - Expected: {Expected}, Received: {Received}", 
                expectedSignature, webhookData.Signature);
            
            // Use time-constant comparison to prevent timing attacks
            return AreSignaturesEqual(expectedSignature, webhookData.Signature.ToLower());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayOS webhook signature");
            return false;
        }
    }

    private static bool AreSignaturesEqual(string a, string b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        var result = 0;
        for (int i = 0; i < a.Length; i++)
            result |= a[i] ^ b[i];
        return result == 0;
    }
}
}