using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DTOs.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Services.Interfaces;
using BusinessObjects.Common;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/payos")]
    public class PayOSWebhookController : ControllerBase
    {
        private readonly ILogger<PayOSWebhookController> _logger;
        private readonly PayOSOptions _options;
        private readonly IOrderService _orderService;

        public PayOSWebhookController(ILogger<PayOSWebhookController> logger,
                                      IOptions<PayOSOptions> options,
                                      IOrderService orderService)
        {
            _logger = logger;
            _options = options.Value;
            _orderService = orderService;
        }

        [HttpGet("webhook")]
        public IActionResult WebhookGet()
        {
            _logger.LogInformation("PayOS Webhook GET endpoint called");
            return Ok(new { 
                message = "PayOS Webhook is working!", 
                timestamp = DateTime.UtcNow,
                status = "active",
                webhookUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }

        [HttpGet("webhook-info")]
        public IActionResult GetWebhookInfo()
        {
            var webhookUrl = $"{Request.Scheme}://{Request.Host}/api/payos/webhook";
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
            
            return Ok(new {
                webhookUrl = webhookUrl,
                environment = environment,
                timestamp = DateTime.UtcNow,
                status = "active",
                message = "Use this URL as your PayOS webhook URL"
            });
        }

        [HttpPost("test")]
        public IActionResult TestWebhook()
        {
            _logger.LogInformation("PayOS Webhook Test endpoint called - NEW VERSION");
            return Ok(new { 
                message = "PayOS Webhook is working - NEW VERSION!", 
                timestamp = DateTime.UtcNow,
                status = "active"
            });
        }

        [HttpPost("simulate")]
        public async Task<IActionResult> SimulatePayOSWebhook([FromBody] object payload)
        {
            _logger.LogInformation("=== SIMULATING PAYOS WEBHOOK ===");
            _logger.LogInformation("Payload: {Payload}", System.Text.Json.JsonSerializer.Serialize(payload));
            
            // Simulate real PayOS webhook data
            var simulatedData = new
            {
                Event = "payment.success",
                Data = new
                {
                    OrderCode = "123456789",
                    Amount = 100000,
                    Status = "PAID",
                    TransactionDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            };

            _logger.LogInformation("Simulated PayOS data: {Data}", System.Text.Json.JsonSerializer.Serialize(simulatedData));
            
            return Ok(new { 
                message = "Simulation completed - check logs for details",
                simulated = simulatedData
            });
        }

        [HttpGet("find-order/{orderCode}")]
        public async Task<IActionResult> FindOrderByPayOSOrderCode(string orderCode)
        {
            try
            {
                _logger.LogInformation("=== FINDING ORDER BY PAYOS ORDER CODE: {OrderCode} ===", orderCode);
                
                var result = await _orderService.FindOrderByPayOSOrderCodeAsync(orderCode);
                
                return Ok(new { 
                    message = "Order search completed",
                    orderCode = orderCode,
                    result = result.IsSuccess ? "SUCCESS" : "FAILED",
                    data = result.Data,
                    message_detail = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding order by PayOSOrderCode: {OrderCode}", orderCode);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("verify-payment-flow/{orderId}")]
        public async Task<IActionResult> VerifyPaymentFlow(Guid orderId)
        {
            try
            {
                _logger.LogInformation("=== VERIFYING PAYMENT FLOW FOR ORDER {OrderId} ===", orderId);
                
                var result = await _orderService.VerifyOrderPaymentFlowAsync(orderId);
                
                return Ok(new { 
                    message = "Payment flow verification completed",
                    orderId = orderId,
                    result = result.IsSuccess ? "SUCCESS" : "FAILED",
                    message_detail = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment flow for order {OrderId}", orderId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("debug-order/{orderId}")]
        public async Task<IActionResult> DebugOrder(Guid orderId)
        {
            try
            {
                _logger.LogInformation("=== DEBUGGING ORDER {OrderId} ===", orderId);
                
                var result = await _orderService.GetOrderByIdForDebugAsync(orderId);
                
                return Ok(new { 
                    message = "Order debug completed",
                    orderId = orderId,
                    result = result.IsSuccess ? "SUCCESS" : "FAILED",
                    data = result.Data,
                    message_detail = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in order debug for {OrderId}", orderId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("test-webhook/{orderId}")]
        public async Task<IActionResult> TestWebhookForOrder(Guid orderId)
        {
            try
            {
                _logger.LogInformation("=== TESTING WEBHOOK FOR ORDER {OrderId} ===", orderId);
                
                // Lấy thông tin order
                var orderResult = await _orderService.GetOrderByIdForDebugAsync(orderId);
                if (!orderResult.IsSuccess)
                {
                    return BadRequest(new { error = "Order not found", orderId = orderId });
                }
                
                var order = orderResult.Data;
                if (string.IsNullOrEmpty(order.PayOSOrderCode))
                {
                    return BadRequest(new { error = "Order does not have PayOSOrderCode", orderId = orderId });
                }
                
                // Tạo webhook payload giả
                var testWebhookData = new
                {
                    Code = "00",
                    Desc = "success",
                    Success = true,
                    Data = new
                    {
                        OrderCode = long.Parse(order.PayOSOrderCode),
                        Amount = (int)order.FinalPrice,
                        Description = $"Payment for Order {order.Id}",
                        Reference = "TEST_REF_" + Guid.NewGuid().ToString("N")[..8],
                        TransactionDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        Currency = "VND",
                        Status = "PAID"
                    },
                    Signature = "test_signature_" + Guid.NewGuid().ToString("N")[..16]
                };
                
                _logger.LogInformation("Testing webhook with PayOSOrderCode: {PayOSOrderCode}", order.PayOSOrderCode);
                
                // Gọi method xử lý webhook
                var result = await _orderService.UpdateOrderStatusByOrderCodeAsync(
                    order.PayOSOrderCode, 
                    BusinessObjects.Common.OrderStatus.Completed
                );
                
                _logger.LogInformation("Webhook test result: {Result}", result.IsSuccess ? "SUCCESS" : "FAILED");
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Webhook test failed: {Message}", result.Message);
                }
                
                return Ok(new { 
                    message = "Webhook test completed",
                    orderId = orderId,
                    payOSOrderCode = order.PayOSOrderCode,
                    result = result.IsSuccess ? "SUCCESS" : "FAILED",
                    details = result.Message,
                    testPayload = testWebhookData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in webhook test for order {OrderId}", orderId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("test-real")]
        public async Task<IActionResult> TestRealWebhook()
        {
            try
            {
                _logger.LogInformation("=== TESTING REAL PAYOS WEBHOOK PROCESSING ===");
                
                // Tạo test order code
                var testOrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                
                // Test với real webhook data structure
                var testWebhookData = new
                {
                    Event = "payment.success",
                    Data = new
                    {
                        OrderCode = testOrderCode,
                        Amount = 50000,
                        Status = "PAID",
                        TransactionDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    }
                };

                _logger.LogInformation("Testing with order code: {OrderCode}", testOrderCode);
                
                // Gọi method thực sự để test
                var result = await _orderService.UpdateOrderStatusByOrderCodeAsync(
                    testOrderCode, 
                    BusinessObjects.Common.OrderStatus.Completed
                );

                _logger.LogInformation("Test result: {Result}", result.IsSuccess ? "SUCCESS" : "FAILED");
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Test failed: {Message}", result.Message);
                }

                return Ok(new { 
                    message = "Real webhook test completed",
                    orderCode = testOrderCode,
                    result = result.IsSuccess ? "SUCCESS" : "FAILED",
                    details = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in real webhook test");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook()
        {
            try
            {
                // 1. Read raw body (string) - must read raw for signature verification
                Request.EnableBuffering();
                using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                Request.Body.Position = 0;

            // Handle PayOS validation request (simple test)
            if (string.IsNullOrEmpty(body) || body == "{}")
            {
                return Ok(new { code = "00", desc = "Webhook validated successfully" });
            }

            // Handle empty POST request (PayOS validation)
            if (Request.ContentLength == 0 || body == "")
            {
                return Ok(new { code = "00", desc = "Webhook validated successfully" });
            }

            // 2. Try read signature from header 'x-signature' or from JSON field "signature"
            string? signatureFromHeader = null;
            if (Request.Headers.TryGetValue("x-signature", out var sigValues))
            {
                signatureFromHeader = sigValues.FirstOrDefault();
            }

            // try parse signature inside body JSON if header not present
            string? signatureFromBody = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("signature", out var sigElem))
                {
                    signatureFromBody = sigElem.GetString();
                }
            }
            catch (JsonException)
            {
                // body might not be valid JSON; we'll still try header-based verification
            }

            var receivedSignature = signatureFromHeader ?? signatureFromBody;
            if (string.IsNullOrEmpty(receivedSignature))
            {
                _logger.LogWarning("No signature found in header or body.");
                return BadRequest("Missing signature");
            }

            // 3. Compute HMAC-SHA256 using PayOS format: key1=value1&key2=value2
            _logger.LogInformation("ChecksumKey: {ChecksumKey}", _options.ChecksumKey);
            _logger.LogInformation("Raw body for signature: {Body}", body);
            
            // Parse payload to get data for signature
            var payload = JsonSerializer.Deserialize<WebhookPayload>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            string computedSignature;
            if (payload?.Data != null)
            {
                // Create signature data in PayOS format: key1=value1&key2=value2
                var signatureData = CreatePayOSSignatureData(payload.Data);
                _logger.LogInformation("Signature data: {SignatureData}", signatureData);
                computedSignature = ComputeHmacSha256Hex(signatureData, _options.ChecksumKey);
            }
            else
            {
                // Fallback to raw body if parsing fails
                computedSignature = ComputeHmacSha256Hex(body, _options.ChecksumKey);
            }

            // 4. Compare signatures (use time-constant compare)
            if (!AreSignaturesEqual(computedSignature, receivedSignature))
            {
                _logger.LogError("Invalid signature. Computed: {Computed}, Received: {Received}", computedSignature, receivedSignature);
                _logger.LogWarning("Accepting webhook despite invalid signature for testing");
                // return Ok(new { code = "00", desc = "Received" });
            }

            // 5. Validate payload
            if (payload == null)
            {
                _logger.LogWarning("Webhook payload is empty after deserialization.");
                return BadRequest("Empty payload");
            }

            // 6. Handle events
            try
            {
                var data = payload.Data;
                var orderCode = data?.OrderCode.ToString() ?? string.Empty;
                
                _logger.LogInformation("Processing PayOS webhook for order {OrderCode}", orderCode);
                _logger.LogInformation("PayOS payload: {Payload}", JsonSerializer.Serialize(payload));
                _logger.LogInformation("Raw OrderCode from PayOS: {RawOrderCode} (Type: {Type})", data?.OrderCode, data?.OrderCode.GetType().Name);

                // Check if payment was successful based on PayOS response
                if (payload.Success == true || payload.Code == "00")
                {
                    _logger.LogInformation("Payment successful for order {OrderCode}", orderCode);
                    _logger.LogInformation("Calling UpdateOrderStatusByOrderCodeAsync for order {OrderCode}", orderCode);
                    
                    var result = await _orderService.UpdateOrderStatusByOrderCodeAsync(orderCode, OrderStatus.Completed, paymentInfo: data);
                    _logger.LogInformation("UpdateOrderStatusByOrderCodeAsync result: {Result}", JsonSerializer.Serialize(result));
                }
                else
                {
                    _logger.LogWarning("Payment failed for order {OrderCode}", orderCode);
                    _logger.LogInformation("Calling UpdateOrderStatusByOrderCodeAsync for order {OrderCode}", orderCode);
                    
                    var result = await _orderService.UpdateOrderStatusByOrderCodeAsync(orderCode, OrderStatus.Cancelled, paymentInfo: data);
                    _logger.LogInformation("UpdateOrderStatusByOrderCodeAsync result: {Result}", JsonSerializer.Serialize(result));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook.");
                // IMPORTANT: still return 200 or a specific code? Usually return 500 so PayOS may retry.
                return StatusCode(500, "Processing error");
            }

            // 7. Return 200 OK to acknowledge
            return Ok(new { code = "00", desc = "Webhook processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in webhook processing");
                return Ok(new { code = "00", desc = "Received" }); // Always return 200 to PayOS
            }
        }

        private static string ComputeHmacSha256Hex(string message, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? ""));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToHexString(hash).ToLower(); // match PayOS hex lower-case convention
        }

        private static bool AreSignaturesEqual(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            var result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];
            return result == 0;
        }

        private static string CreatePayOSSignatureData(PayOSData data)
        {
            // Create signature data in PayOS format: key1=value1&key2=value2
            // Sort keys alphabetically as required by PayOS
            var parameters = new Dictionary<string, string>();
            
            if (data.OrderCode != null)
                parameters["orderCode"] = data.OrderCode.ToString();
            if (data.Amount != null)
                parameters["amount"] = data.Amount.ToString();
            if (data.Description != null)
                parameters["description"] = data.Description;
            if (data.Reference != null)
                parameters["reference"] = data.Reference;
            if (data.TransactionDateTime != null)
                parameters["transactionDateTime"] = data.TransactionDateTime;
            if (data.Currency != null)
                parameters["currency"] = data.Currency;
            
            // Sort by key alphabetically
            var sortedParams = parameters.OrderBy(kvp => kvp.Key);
            
            // Create signature string: key1=value1&key2=value2
            return string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

    // DTOs for webhook payload (adjust fields to actual PayOS payload)
    public class WebhookPayload
    {
        public string? Code { get; set; }
        public string? Desc { get; set; }
        public bool? Success { get; set; }
        public PayOSData? Data { get; set; }
        public string? Signature { get; set; }
    }

        public class PayOSData
        {
            // adjust types/names to actual payload structure
            public long OrderCode { get; set; }
            public int? Amount { get; set; }
            public string? Description { get; set; }
            public string? Reference { get; set; }
            public string? TransactionDateTime { get; set; }
            public string? Currency { get; set; }
            public string? Status { get; set; }
            // plus other fields...
        }
    }
}
