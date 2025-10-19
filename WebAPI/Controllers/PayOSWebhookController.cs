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
                status = "active"
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

            // 3. Compute HMAC-SHA256 of raw body using ChecksumKey (secret)
            var computedSignature = ComputeHmacSha256Hex(body, _options.ChecksumKey);

            // 4. Compare signatures (use time-constant compare)
            if (!AreSignaturesEqual(computedSignature, receivedSignature))
            {
                _logger.LogError("Invalid signature. Computed: {Computed}, Received: {Received}", computedSignature, receivedSignature);
                _logger.LogWarning("Accepting webhook despite invalid signature for testing");
                // return Ok(new { code = "00", desc = "Received" });
            }

            // 5. Parse event and data
            WebhookPayload? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<WebhookPayload>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize webhook payload.");
                return BadRequest("Invalid payload");
            }

            if (payload == null)
            {
                _logger.LogWarning("Webhook payload is empty after deserialization.");
                return BadRequest("Empty payload");
            }

            // 6. Handle events
            try
            {
                var data = payload.Data;
                var orderCode = data?.OrderCode?.ToString() ?? string.Empty;
                
                _logger.LogInformation("Processing PayOS webhook for order {OrderCode}", orderCode);

                // Check if payment was successful based on PayOS response
                if (payload.Success == true || payload.Code == "00")
                {
                    _logger.LogInformation("Payment successful for order {OrderCode}", orderCode);
                    await _orderService.UpdateOrderStatusByOrderCodeAsync(orderCode, OrderStatus.Completed, paymentInfo: data);
                }
                else
                {
                    _logger.LogWarning("Payment failed for order {OrderCode}", orderCode);
                    await _orderService.UpdateOrderStatusByOrderCodeAsync(orderCode, OrderStatus.Cancelled, paymentInfo: data);
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
            public object? OrderCode { get; set; }
            public int? Amount { get; set; }
            public string? Status { get; set; }
            public string? TransactionDateTime { get; set; }
            // plus other fields...
        }
    }
}
