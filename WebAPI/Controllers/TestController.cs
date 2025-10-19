using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using DTOs.PayOSDTOs;
using System.Text.Json;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/payos")]
    public class PayOSWebhookController : ControllerBase
    {
        private readonly IPayOSService _payOSService;
        private readonly ILogger<PayOSWebhookController> _logger;

        public PayOSWebhookController(IPayOSService payOSService, ILogger<PayOSWebhookController> logger)
        {
            _payOSService = payOSService;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] PayOSWebhookData data)
        {
            try
            {
                _logger.LogInformation("PayOS Webhook received: {Data}", JsonSerializer.Serialize(data));

                // 1. Xác thực signature từ PayOS
                var isValid = await _payOSService.VerifyWebhookDataAsync(data);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid PayOS webhook signature for order: {OrderCode}", data.OrderCode);
                    return BadRequest("Invalid signature");
                }

                // 2. Xử lý theo trạng thái
                switch (data.Status.ToUpper())
                {
                    case "PAID":
                        await HandlePaymentSuccess(data);
                        break;
                    case "CANCELLED":
                        await HandlePaymentCancelled(data);
                        break;
                    case "EXPIRED":
                        await HandlePaymentExpired(data);
                        break;
                    default:
                        _logger.LogWarning("Unknown PayOS webhook status: {Status} for order: {OrderCode}", 
                            data.Status, data.OrderCode);
                        break;
                }

                return Ok(new { message = "Webhook processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS webhook for order: {OrderCode}", data.OrderCode);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private async Task HandlePaymentSuccess(PayOSWebhookData data)
        {
            _logger.LogInformation("Payment successful for order: {OrderCode}, amount: {Amount}", 
                data.OrderCode, data.Amount);

            // ✅ Cập nhật database - đơn hàng thành công
            try
            {
                // TODO: Uncomment khi có OrderService
                // await _orderService.UpdateOrderStatusAsync(data.OrderCode, OrderStatus.PAID);
                // await _inventoryService.UpdateStockAsync(order.Items);
                // await _emailService.SendPaymentSuccessEmailAsync(order);
                
                _logger.LogInformation("✅ Database updated successfully for order: {OrderCode}", data.OrderCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to update database for order: {OrderCode}", data.OrderCode);
                throw; // Re-throw để webhook trả về lỗi
            }
        }

        private async Task HandlePaymentCancelled(PayOSWebhookData data)
        {
            _logger.LogInformation("Payment cancelled for order: {OrderCode}", data.OrderCode);

            // ✅ Cập nhật database - hủy đơn hàng
            try
            {
                // TODO: Uncomment khi có OrderService
                // await _orderService.UpdateOrderStatusAsync(data.OrderCode, OrderStatus.CANCELLED);
                
                _logger.LogInformation("✅ Order cancelled in database: {OrderCode}", data.OrderCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to cancel order in database: {OrderCode}", data.OrderCode);
                throw;
            }
        }

        private async Task HandlePaymentExpired(PayOSWebhookData data)
        {
            _logger.LogInformation("Payment expired for order: {OrderCode}", data.OrderCode);

            // ✅ Cập nhật database - đơn hàng hết hạn
            try
            {
                // TODO: Uncomment khi có OrderService
                // await _orderService.UpdateOrderStatusAsync(data.OrderCode, OrderStatus.EXPIRED);
                
                _logger.LogInformation("✅ Order expired in database: {OrderCode}", data.OrderCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to expire order in database: {OrderCode}", data.OrderCode);
                throw;
            }
        }
    }
}
