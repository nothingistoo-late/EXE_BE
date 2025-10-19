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

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook()
        {
            // 1. Read raw body (string) - must read raw for signature verification
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            _logger.LogInformation("Received PayOS webhook body: {Body}", body);

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
                _logger.LogWarning("Invalid signature. Computed: {Computed}, Received: {Received}", computedSignature, receivedSignature);
                return BadRequest("Invalid signature");
            }

            _logger.LogInformation("Webhook signature verified.");

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
                // Example: data.OrderCode assumed numeric or string - adapt to your model
                var orderCode = data?.OrderCode?.ToString() ?? string.Empty;

                _logger.LogInformation("Handling PayOS event {Event} for order {OrderCode}", payload.Event, orderCode);

                switch (payload.Event?.ToLowerInvariant())
                {
                    case "payment.success":
                    case "payment.paid":
                    case "payment.completed":
                        // update order status to Completed
                        await _orderService.UpdateOrderStatusByOrderCodeAsync(orderCode, OrderStatus.Completed, paymentInfo: data);
                        break;

                    case "payment.cancelled":
                    case "payment.failed":
                        await _orderService.UpdateOrderStatusByOrderCodeAsync(orderCode, OrderStatus.Cancelled, paymentInfo: data);
                        break;

                    case "payment.expired":
                        await _orderService.UpdateOrderStatusByOrderCodeAsync(orderCode, OrderStatus.Pending, paymentInfo: data);
                        break;

                    default:
                        _logger.LogWarning("Unhandled PayOS event type: {Event}", payload.Event);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook.");
                // IMPORTANT: still return 200 or a specific code? Usually return 500 so PayOS may retry.
                return StatusCode(500, "Processing error");
            }

            // 7. Return 200 OK to acknowledge
            return Ok(new { code = "00", desc = "Received" });
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
            public string? Event { get; set; }
            public object? Signature { get; set; } // sometimes included
            public PayOSData? Data { get; set; }
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
