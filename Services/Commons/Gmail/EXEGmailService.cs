using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects;
using Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Services.Commons.Gmail
{
    public class EXEGmailService : IEXEGmailService
    {
        private IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EXEGmailService> _logger;

        public EXEGmailService(IUnitOfWork unitOfWork, IOptions<EmailSettings> emailSettings,IEmailService emailService, ILogger<EXEGmailService> logger)
        {
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
            _unitOfWork = unitOfWork;
            _logger = logger;

        }
        // Implementation của IEmailService
        public async Task SendRegistrationSuccessEmailAsync(string toEmail, string userName)
        {
            var subject = $"Welcome {userName}!";
            var body = BuildRegistrationSuccessEmailBody(userName);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendOrderConfirmationEmailAsync(string toEmail, Order order)
        {
            try
            {
                _logger.LogInformation("📧 EXEGmailService: SendOrderConfirmationEmailAsync called for order {OrderId}, email: {Email}", 
                    order.Id, toEmail);
                
                if (string.IsNullOrEmpty(toEmail))
                {
                    _logger.LogWarning("❌ Cannot send order confirmation email: toEmail is null or empty for order {OrderId}", order.Id);
                    throw new ArgumentException("Email cannot be null or empty", nameof(toEmail));
                }
                
                // Load order with details and box types
                _logger.LogInformation("📧 Loading order details for order {OrderId}", order.Id);
                var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
                if (orderWithDetails == null)
                {
                    _logger.LogWarning("❌ Order {OrderId} not found when sending confirmation email", order.Id);
                    throw new Exception($"Order {order.Id} not found");
                }
                _logger.LogInformation("✅ Order details loaded successfully for order {OrderId}", order.Id);

                var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
                var shortOrderId = MaskId(order.Id);
                var subject = $"Xác nhận đơn hàng - #{shortOrderId}";
                _logger.LogInformation("📧 Building email body for order {OrderId}", order.Id);
                var body = BuildOrderConfirmationEmailBody(orderWithDetails, userName, maskedUserId, shortOrderId, address);
                _logger.LogInformation("📧 Email body built successfully. Length: {Length} bytes", body.Length);

                _logger.LogInformation("📧 Sending email via IEmailService for order {OrderId}", order.Id);
                await _emailService.SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("✅ EXEGmailService: Order confirmation email sent successfully for order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌❌❌ EXEGmailService ERROR in SendOrderConfirmationEmailAsync - Order: {OrderId}, Email: {Email}", 
                    order.Id, toEmail);
                _logger.LogError("❌ Error Message: {ErrorMessage}", ex.Message);
                _logger.LogError("❌ Error Type: {ErrorType}", ex.GetType().Name);
                if (ex.InnerException != null)
                {
                    _logger.LogError("❌ Inner Exception: {InnerException}", ex.InnerException.Message);
                }
                _logger.LogError("❌ Stack Trace: {StackTrace}", ex.StackTrace);
                throw; // Re-throw để layer trên có thể log
            }
        }

        public async Task SendPaymentSuccessEmailAsync(string toEmail, Order order)
        {
            try
            {
                _logger.LogInformation("📧 EXEGmailService: SendPaymentSuccessEmailAsync called for order {OrderId}, email: {Email}", 
                    order.Id, toEmail);
                
                if (string.IsNullOrEmpty(toEmail))
                {
                    _logger.LogWarning("❌ Cannot send payment success email: toEmail is null or empty for order {OrderId}", order.Id);
                    throw new ArgumentException("Email cannot be null or empty", nameof(toEmail));
                }
                
                // Load order with details and box types
                _logger.LogInformation("📧 Loading order details for order {OrderId}", order.Id);
                var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
                if (orderWithDetails == null)
                {
                    _logger.LogWarning("❌ Order {OrderId} not found when sending payment success email", order.Id);
                    throw new Exception($"Order {order.Id} not found");
                }
                _logger.LogInformation("✅ Order details loaded successfully for order {OrderId}", order.Id);
                
                var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
                var shortOrderId = MaskId(order.Id);
                var subject = $"Thanh toán thành công - #{shortOrderId}";
                _logger.LogInformation("📧 Building email body for order {OrderId}", order.Id);
                var body = BuildPaymentSuccessEmailBody(orderWithDetails, userName, maskedUserId, shortOrderId, address);
                _logger.LogInformation("📧 Email body built successfully. Length: {Length} bytes", body.Length);
                
                _logger.LogInformation("📧 Sending email via IEmailService for order {OrderId}", order.Id);
                await _emailService.SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("✅ EXEGmailService: Payment success email sent successfully for order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌❌❌ EXEGmailService ERROR in SendPaymentSuccessEmailAsync - Order: {OrderId}, Email: {Email}", 
                    order.Id, toEmail);
                _logger.LogError("❌ Error Message: {ErrorMessage}", ex.Message);
                _logger.LogError("❌ Error Type: {ErrorType}", ex.GetType().Name);
                if (ex.InnerException != null)
                {
                    _logger.LogError("❌ Inner Exception: {InnerException}", ex.InnerException.Message);
                }
                _logger.LogError("❌ Stack Trace: {StackTrace}", ex.StackTrace);
                throw; // Re-throw để layer trên có thể log
            }
        }

        public async Task SendNewOrderNotificationToAdminAsync(Order order)
        {
            try
            {
                _logger.LogInformation("📧 EXEGmailService: SendNewOrderNotificationToAdminAsync called for order {OrderId}, admin email: {AdminEmail}", 
                    order.Id, _emailSettings.AdminEmail);
                
                // Load order with details and box types
                _logger.LogInformation("📧 Loading order details for order {OrderId}", order.Id);
                var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
                if (orderWithDetails == null)
                {
                    _logger.LogWarning("❌ Order {OrderId} not found when sending admin notification", order.Id);
                    throw new Exception($"Order {order.Id} not found");
                }
                _logger.LogInformation("✅ Order details loaded successfully for order {OrderId}", order.Id);

                var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
                var shortOrderId = MaskId(order.Id);
                var subject = $"Đơn hàng mới - #{shortOrderId}";
                _logger.LogInformation("📧 Building email body for admin notification - order {OrderId}", order.Id);
                var body = BuildNewOrderNotificationBody(orderWithDetails, userName, maskedUserId, phone, address, shortOrderId);
                _logger.LogInformation("📧 Email body built successfully. Length: {Length} bytes", body.Length);

                _logger.LogInformation("📧 Sending admin notification email for order {OrderId}", order.Id);
                await _emailService.SendEmailAsync(_emailSettings.AdminEmail, subject, body);
                _logger.LogInformation("✅ EXEGmailService: Admin notification email sent successfully for order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌❌❌ EXEGmailService ERROR in SendNewOrderNotificationToAdminAsync - Order: {OrderId}", order.Id);
                _logger.LogError("❌ Error Message: {ErrorMessage}", ex.Message);
                _logger.LogError("❌ Error Type: {ErrorType}", ex.GetType().Name);
                if (ex.InnerException != null)
                {
                    _logger.LogError("❌ Inner Exception: {InnerException}", ex.InnerException.Message);
                }
                _logger.LogError("❌ Stack Trace: {StackTrace}", ex.StackTrace);
                throw;
            }
        }

        public async Task SendCustomEmailAsync(string toEmail, string subject, string body)
        {
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        // ========== EMAIL LIÊN QUAN ĐẾN ĐƠN HÀNG ==========

        public async Task SendOrderPreparationEmailAsync(string toEmail, Order order)
        {
            // Load order with details and box types
            var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
            if (orderWithDetails == null)
            {
                _logger.LogWarning("Order {OrderId} not found when sending preparation email", order.Id);
                return;
            }

            var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
            var shortOrderId = MaskId(order.Id);
            var subject = $"Đơn hàng đang chuẩn bị - #{shortOrderId}";
            var body = BuildOrderPreparationEmailBody(orderWithDetails, userName, maskedUserId, shortOrderId, address);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendOrderDeliveredEmailAsync(string toEmail, Order order)
        {
            // Load order with details and box types
            var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
            if (orderWithDetails == null)
            {
                _logger.LogWarning("Order {OrderId} not found when sending delivered email", order.Id);
                return;
            }

            var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
            var shortOrderId = MaskId(order.Id);
            var subject = $"Giao hàng thành công - #{shortOrderId}";
            var body = BuildOrderDeliveredEmailBody(orderWithDetails, userName, maskedUserId, shortOrderId, address);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendOrderCancelledEmailAsync(string toEmail, Order order, string reason)
        {
            // Load order with details and box types
            var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
            if (orderWithDetails == null)
            {
                _logger.LogWarning("Order {OrderId} not found when sending cancelled email", order.Id);
                return;
            }

            var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
            var shortOrderId = MaskId(order.Id);
            var subject = $"Đơn hàng đã hủy - #{shortOrderId}";
            var body = BuildOrderCancelledEmailBody(orderWithDetails, reason, userName, maskedUserId, shortOrderId, address);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendRefundProcessedEmailAsync(string toEmail, Order order, decimal refundAmount)
        {
            // Load order with details and box types
            var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
            if (orderWithDetails == null)
            {
                _logger.LogWarning("Order {OrderId} not found when sending refund email", order.Id);
                return;
            }

            var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
            var shortOrderId = MaskId(order.Id);
            var subject = $"Hoàn tiền thành công - #{shortOrderId}";
            var body = BuildRefundProcessedEmailBody(orderWithDetails, refundAmount, userName, maskedUserId, shortOrderId, address);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        // ========== EMAIL LIÊN QUAN ĐẾN TÀI KHOẢN ==========

        public async Task SendEmailVerificationEmailAsync(string toEmail, string userName, string verificationLink)
        {
            var subject = "Xác thực email tài khoản";
            var body = BuildEmailVerificationEmailBody(userName, verificationLink);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendForgotPasswordOTPEmailAsync(string toEmail, string userName, string otpCode)
        {
            var subject = "Mã OTP đặt lại mật khẩu";
            var body = BuildForgotPasswordOTPEmailBody(userName, otpCode);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordChangedEmailAsync(string toEmail, string userName)
        {
            var subject = "Mật khẩu đã được thay đổi thành công";
            var body = BuildPasswordChangedEmailBody(userName);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendAccountUpdatedEmailAsync(string toEmail, string userName, string changes)
        {
            var subject = "Thông tin tài khoản đã được cập nhật";
            var body = BuildAccountUpdatedEmailBody(userName, changes);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        // ========== EMAIL MARKETING & CHĂM SÓC KHÁCH HÀNG ==========

        public async Task SendAbandonedCartEmailAsync(string toEmail, string userName, List<OrderDetail> cartItems)
        {
            var subject = "Bạn có sản phẩm chưa hoàn tất thanh toán";
            var body = BuildAbandonedCartEmailBody(userName, cartItems);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        // ========== EMAIL HỆ THỐNG ==========

        public async Task SendSystemMaintenanceEmailAsync(string toEmail, string userName, DateTime maintenanceStart, DateTime maintenanceEnd)
        {
            var subject = "Thông báo bảo trì hệ thống";
            var body = BuildSystemMaintenanceEmailBody(userName, maintenanceStart, maintenanceEnd);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        // ========== CẢNH BÁO VỀ ĐƠN HÀNG (GỬI CHO ADMIN) ==========

        public async Task SendHighValueOrderAlertAsync(Order order, decimal threshold)
        {
            // Load order with details and box types
            var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
            if (orderWithDetails == null)
            {
                _logger.LogWarning("Order {OrderId} not found when sending high value alert", order.Id);
                return;
            }

            var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
            var shortOrderId = MaskId(order.Id);
            var subject = $"🚨 Đơn hàng giá trị cao - #{shortOrderId}";
            var body = BuildHighValueOrderAlertBody(orderWithDetails, threshold, userName, maskedUserId, phone, address, shortOrderId);
            await _emailService.SendEmailAsync(_emailSettings.AdminEmail, subject, body);
        }

        public async Task SendOrderCancelledAlertAsync(Order order, string reason)
        {
            // Load order with details and box types
            var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
            if (orderWithDetails == null)
            {
                _logger.LogWarning("Order {OrderId} not found when sending cancelled alert", order.Id);
                return;
            }

            var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
            var shortOrderId = MaskId(order.Id);
            var subject = $"⚠️ Đơn hàng bị hủy - #{shortOrderId}";
            var body = BuildOrderCancelledAlertBody(orderWithDetails, reason, userName, maskedUserId, phone, address, shortOrderId);
            await _emailService.SendEmailAsync(_emailSettings.AdminEmail, subject, body);
        }

        public async Task SendPendingOrderAlertAsync(Order order, TimeSpan pendingTime)
        {
            // Load order with details and box types
            var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
            if (orderWithDetails == null)
            {
                _logger.LogWarning("Order {OrderId} not found when sending pending alert", order.Id);
                return;
            }

            var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
            var shortOrderId = MaskId(order.Id);
            var subject = $"⏰ Đơn hàng chờ xác nhận - #{shortOrderId}";
            var body = BuildPendingOrderAlertBody(orderWithDetails, pendingTime, userName, maskedUserId, phone, address, shortOrderId);
            await _emailService.SendEmailAsync(_emailSettings.AdminEmail, subject, body);
        }

        public async Task SendPaymentFailedAlertAsync(Order order, int failureCount)
        {
            // Load order with details and box types
            var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
            if (orderWithDetails == null)
            {
                _logger.LogWarning("Order {OrderId} not found when sending payment failed alert", order.Id);
                return;
            }

            var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
            var shortOrderId = MaskId(order.Id);
            var subject = $"💳 Thanh toán thất bại nhiều lần - #{shortOrderId}";
            var body = BuildPaymentFailedAlertBody(orderWithDetails, failureCount, userName, maskedUserId, phone, address, shortOrderId);
            await _emailService.SendEmailAsync(_emailSettings.AdminEmail, subject, body);
        }

        public async Task SendRefundRequestAlertAsync(Order order, string reason)
        {
            // Load order with details and box types
            var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
            if (orderWithDetails == null)
            {
                _logger.LogWarning("Order {OrderId} not found when sending refund request alert", order.Id);
                return;
            }

            var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
            var shortOrderId = MaskId(order.Id);
            var subject = $"💰 Yêu cầu hoàn tiền - #{shortOrderId}";
            var body = BuildRefundRequestAlertBody(orderWithDetails, reason, userName, maskedUserId, phone, address, shortOrderId);
            await _emailService.SendEmailAsync(_emailSettings.AdminEmail, subject, body);
        }

        public async Task SendDeliveryIssueAlertAsync(Order order, string issue)
        {
            // Load order with details and box types
            var orderWithDetails = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(order.Id);
            if (orderWithDetails == null)
            {
                _logger.LogWarning("Order {OrderId} not found when sending delivery issue alert", order.Id);
                return;
            }

            var (userName, maskedUserId, phone, address) = await GetUserInfoAsync(order.UserId);
            var shortOrderId = MaskId(order.Id);
            var subject = $"🚚 Sự cố giao hàng - #{shortOrderId}";
            var body = BuildDeliveryIssueAlertBody(orderWithDetails, issue, userName, maskedUserId, phone, address, shortOrderId);
            await _emailService.SendEmailAsync(_emailSettings.AdminEmail, subject, body);
        }

        // Private methods để build HTML body
        private string BuildRegistrationSuccessEmailBody(string userName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to MyApp!</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p>Cảm ơn bạn đã đăng ký tài khoản tại MyApp!</p>
            <p>Tài khoản của bạn đã được kích hoạt thành công. Bạn có thể đăng nhập và bắt đầu sử dụng dịch vụ của chúng tôi.</p>
            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildOrderConfirmationEmailBody(Order order, string userName, string maskedUserId, string shortOrderId, string address)
        {
            var itemsHtml = string.Join("", order.OrderDetails.Select(item => $@"
            <tr>
                <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.BoxType?.Name ?? "Unknown Product"}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{item.UnitPrice:N0} VNĐ</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{(item.Quantity * item.UnitPrice):N0} VNĐ</td>
            </tr>"));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th {{ background-color: #2196F3; color: white; padding: 10px; text-align: left; }}
        .total {{ font-size: 18px; font-weight: bold; color: #2196F3; text-align: right; padding: 10px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Xác nhận đơn hàng</h1>
            <p>Đơn hàng #${shortOrderId}</p>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p><strong>Mã khách hàng:</strong> {maskedUserId}</p>
            <p><strong>Địa chỉ giao hàng:</strong> {address}</p>
            <p>Cảm ơn bạn đã đặt hàng! Đơn hàng của bạn đã được xác nhận.</p>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Trạng thái:</strong> {order.Status}</p>
            <p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
            
            <h3>Chi tiết sản phẩm:</h3>
            <table>
                <thead>
                    <tr>
                        <th>Sản phẩm</th>
                        <th style='text-align: center;'>Số lượng</th>
                        <th style='text-align: right;'>Đơn giá</th>
                        <th style='text-align: right;'>Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>
            
            <div class='total'>
                Tổng cộng: {order.FinalPrice:N0} VNĐ
            </div>
            
            <p>Chúng tôi sẽ xử lý đơn hàng của bạn trong thời gian sớm nhất.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildPaymentSuccessEmailBody(Order order, string userName, string maskedUserId, string shortOrderId, string address)
        {
            var itemsHtml = string.Join("", order.OrderDetails.Select(item => $@"
            <tr>
                <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.BoxType?.Name ?? "Unknown Product"}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{item.UnitPrice:N0} VNĐ</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{(item.Quantity * item.UnitPrice):N0} VNĐ</td>
            </tr>"));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .success-badge {{ background-color: #4CAF50; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th {{ background-color: #4CAF50; color: white; padding: 10px; text-align: left; }}
        .total {{ font-size: 18px; font-weight: bold; color: #4CAF50; text-align: right; padding: 10px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Thanh toán thành công</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p><strong>Mã khách hàng:</strong> {maskedUserId}</p>
            <p><strong>Địa chỉ giao hàng:</strong> {address}</p>
            <div class='success-badge'>Thanh toán thành công!</div>
            <p>Chúng tôi đã nhận được thanh toán của bạn. Đơn hàng đang được xử lý.</p>
            
            <h3>Thông tin thanh toán:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>Ngày thanh toán:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Phương thức:</strong> {order.PaymentMethod}</p>
            <p><strong>Trạng thái:</strong> {order.Status}</p>
            
            <h3>Chi tiết hóa đơn:</h3>
            <table>
                <thead>
                    <tr>
                        <th>Sản phẩm</th>
                        <th style='text-align: center;'>Số lượng</th>
                        <th style='text-align: right;'>Đơn giá</th>
                        <th style='text-align: right;'>Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>
            
            <div class='total'>
                Tổng thanh toán: {order.FinalPrice:N0} VNĐ
            </div>
            
            <p>Đơn hàng sẽ được giao trong vòng 3-5 ngày làm việc.</p>
            <p>Cảm ơn bạn đã mua hàng tại MyApp!</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildNewOrderNotificationBody(Order order, string userName, string maskedUserId, string phone, string address, string shortOrderId)
        {
            var itemsHtml = string.Join("", order.OrderDetails.Select(item => $@"
            <tr>
                <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.BoxType?.Name ?? "Unknown Product"}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{item.UnitPrice:N0} VNĐ</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{(item.Quantity * item.UnitPrice):N0} VNĐ</td>
            </tr>"));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert {{ background-color: #FF9800; color: white; padding: 10px; border-radius: 5px; margin: 20px 0; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th {{ background-color: #FF9800; color: white; padding: 10px; text-align: left; }}
        .total {{ font-size: 18px; font-weight: bold; color: #FF9800; text-align: right; padding: 10px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔔 Đơn hàng mới</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <div class='alert'>
                <strong>⚠️ Có đơn hàng mới cần xử lý!</strong>
            </div>
            
            <h3>Thông tin khách hàng:</h3>
            <p><strong>User ID:</strong> {maskedUserId}</p>
            <p><strong>Tên:</strong> {userName}</p>
            <p><strong>Số điện thoại:</strong> {phone}</p>
            <p><strong>Địa chỉ:</strong> {address}</p>
            <p><strong>Trạng thái:</strong> {order.Status}</p>
            <p><strong>Đã thanh toán:</strong> {(order.IsPaid ? "Có" : "Chưa")}</p>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
            <p><strong>Phương thức giao hàng:</strong> {order.DeliveryMethod}</p>
            
            <h3>Chi tiết sản phẩm:</h3>
            <table>
                <thead>
                    <tr>
                        <th>Sản phẩm</th>
                        <th style='text-align: center;'>Số lượng</th>
                        <th style='text-align: right;'>Đơn giá</th>
                        <th style='text-align: right;'>Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>
            
            <div class='total'>
                Tổng giá trị: {order.FinalPrice:N0} VNĐ
            </div>
            
            <p><strong>Vui lòng xử lý đơn hàng này trong thời gian sớm nhất.</strong></p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp - Admin Notification System</p>
        </div>
    </div>
</body>
</html>";
        }

        private async Task<(string userName, string maskedUserId, string phone, string address)> GetUserInfoAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                var customer = await _unitOfWork.CustomerRepository
                    .GetQueryable()
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                var name = "Khách hàng";
                if (user != null)
                {
                    var full = (user.FullName ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(full))
                    {
                        name = full;
                    }
                    else
                    {
                        var combined = ($"{user.FirstName} {user.LastName}").Trim();
                        if (!string.IsNullOrWhiteSpace(combined))
                            name = combined;
                    }
                }
                var phone = user?.PhoneNumber ?? "N/A";
                var address = customer?.Address ?? "(chưa cập nhật)";
                var masked = userId.ToString();
                if (!string.IsNullOrWhiteSpace(masked))
                {
                    masked = masked.Length >= 5 ? masked.Substring(0, 5) : masked;
                }
                return (name, masked, phone, address);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user info for {UserId}", userId);
                var masked = userId.ToString();
                masked = masked.Length >= 5 ? masked.Substring(0, 5) : masked;
                return ("Khách hàng", masked, "N/A", "(chưa cập nhật)");
            }
        }

        private string MaskId(Guid id)
        {
            var s = id.ToString();
            return s.Length >= 5 ? s.Substring(0, 5) : s;
        }

        // ========== TEMPLATE EMAIL CHO ĐƠN HÀNG ==========

        private string BuildOrderPreparationEmailBody(Order order, string userName, string maskedUserId, string shortOrderId, string address)
        {
            var itemsHtml = string.Join("", order.OrderDetails.Select(item => $@"
            <tr>
                <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.BoxType?.Name ?? "Unknown Product"}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{item.UnitPrice:N0} VNĐ</td>
            </tr>"));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .preparation-badge {{ background-color: #FF9800; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th {{ background-color: #FF9800; color: white; padding: 10px; text-align: left; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📦 Đơn hàng đang được chuẩn bị</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p><strong>Mã khách hàng:</strong> {maskedUserId}</p>
            <p><strong>Địa chỉ giao hàng:</strong> {address}</p>
            <div class='preparation-badge'>Đơn hàng đang được đóng gói!</div>
            <p>Đơn hàng của bạn đã được xác nhận và đang được chuẩn bị để giao hàng.</p>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Trạng thái:</strong> Đang chuẩn bị</p>
            <p><strong>Dự kiến giao hàng:</strong> 2-3 ngày làm việc</p>
            
            <h3>Sản phẩm đang được chuẩn bị:</h3>
            <table>
                <thead>
                    <tr>
                        <th>Sản phẩm</th>
                        <th style='text-align: center;'>Số lượng</th>
                        <th style='text-align: right;'>Đơn giá</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>
            
            <p>Chúng tôi sẽ thông báo khi đơn hàng được giao cho đơn vị vận chuyển.</p>
            <p>Cảm ơn bạn đã tin tưởng và mua hàng tại MyApp!</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildOrderDeliveredEmailBody(Order order, string userName, string maskedUserId, string shortOrderId, string address)
        {
            var itemsHtml = string.Join("", order.OrderDetails.Select(item => $@"
            <tr>
                <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.BoxType?.Name ?? "Unknown Product"}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{item.UnitPrice:N0} VNĐ</td>
            </tr>"));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .delivered-badge {{ background-color: #4CAF50; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th {{ background-color: #4CAF50; color: white; padding: 10px; text-align: left; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Giao hàng thành công</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p><strong>Mã khách hàng:</strong> {maskedUserId}</p>
            <p><strong>Địa chỉ giao hàng:</strong> {address}</p>
            <div class='delivered-badge'>Đơn hàng đã được giao thành công!</div>
            <p>Chúng tôi rất vui thông báo rằng đơn hàng của bạn đã được giao thành công.</p>
            
            <h3>Thông tin giao hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>Ngày giao hàng:</strong> {DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm}</p>
            <p><strong>Trạng thái:</strong> Đã giao hàng</p>
            
            <h3>Sản phẩm đã giao:</h3>
            <table>
                <thead>
                    <tr>
                        <th>Sản phẩm</th>
                        <th style='text-align: center;'>Số lượng</th>
                        <th style='text-align: right;'>Đơn giá</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>
            
            <p>Nếu bạn hài lòng với sản phẩm, hãy để lại đánh giá để giúp chúng tôi cải thiện dịch vụ.</p>
            <p>Cảm ơn bạn đã tin tưởng và mua hàng tại MyApp!</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildOrderCancelledEmailBody(Order order, string reason, string userName, string maskedUserId, string shortOrderId, string address)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .cancelled-badge {{ background-color: #f44336; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .reason-box {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Đơn hàng đã bị hủy</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p><strong>Mã khách hàng:</strong> {maskedUserId}</p>
            <p><strong>Địa chỉ giao hàng:</strong> {address}</p>
            <div class='cancelled-badge'>Đơn hàng đã bị hủy</div>
            <p>Chúng tôi rất tiếc thông báo rằng đơn hàng của bạn đã bị hủy.</p>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Trạng thái:</strong> Đã hủy</p>
            <p><strong>Tổng giá trị:</strong> {order.FinalPrice:N0} VNĐ</p>
            
            <div class='reason-box'>
                <h4>Lý do hủy đơn hàng:</h4>
                <p>{reason}</p>
            </div>
            
            <p>Nếu bạn đã thanh toán, số tiền sẽ được hoàn lại trong vòng 3-5 ngày làm việc.</p>
            <p>Chúng tôi xin lỗi vì sự bất tiện này. Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildRefundProcessedEmailBody(Order order, decimal refundAmount, string userName, string maskedUserId, string shortOrderId, string address)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .refund-badge {{ background-color: #2196F3; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .amount-box {{ background-color: #e3f2fd; border: 1px solid #2196F3; padding: 15px; border-radius: 5px; margin: 20px 0; text-align: center; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💰 Hoàn tiền thành công</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p><strong>Mã khách hàng:</strong> {maskedUserId}</p>
            <p><strong>Địa chỉ giao hàng:</strong> {address}</p>
            <div class='refund-badge'>Hoàn tiền đã được xử lý!</div>
            <p>Chúng tôi đã xử lý yêu cầu hoàn tiền của bạn thành công.</p>
            
            <h3>Thông tin hoàn tiền:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>Ngày hoàn tiền:</strong> {DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm}</p>
            <p><strong>Phương thức thanh toán gốc:</strong> {order.PaymentMethod}</p>
            
            <div class='amount-box'>
                <h3>Số tiền hoàn lại:</h3>
                <h2 style='color: #2196F3; margin: 0;'>{refundAmount:N0} VNĐ</h2>
            </div>
            
            <p><strong>Thời gian nhận tiền:</strong></p>
            <ul>
                <li>Thẻ tín dụng/ghi nợ: 3-5 ngày làm việc</li>
                <li>Ví điện tử: 1-2 ngày làm việc</li>
                <li>Chuyển khoản ngân hàng: 2-3 ngày làm việc</li>
            </ul>
            
            <p>Nếu bạn không nhận được tiền trong thời gian trên, vui lòng liên hệ với chúng tôi.</p>
            <p>Cảm ơn bạn đã tin tưởng MyApp!</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        // ========== TEMPLATE EMAIL CHO TÀI KHOẢN ==========

        private string BuildEmailVerificationEmailBody(string userName, string verificationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #9C27B0; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .verify-button {{ background-color: #9C27B0; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📧 Xác thực Email</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p>Cảm ơn bạn đã đăng ký tài khoản tại MyApp!</p>
            <p>Để hoàn tất quá trình đăng ký, vui lòng xác thực địa chỉ email của bạn bằng cách nhấn vào nút bên dưới:</p>
            
            <div style='text-align: center;'>
                <a href='{verificationLink}' class='verify-button'>Xác thực Email</a>
            </div>
            
            <p>Nếu nút không hoạt động, bạn có thể sao chép và dán liên kết sau vào trình duyệt:</p>
            <p style='word-break: break-all; background-color: #f0f0f0; padding: 10px; border-radius: 5px;'>{verificationLink}</p>
            
            <p><strong>Lưu ý:</strong> Liên kết này sẽ hết hạn sau 24 giờ.</p>
            <p>Nếu bạn không tạo tài khoản này, vui lòng bỏ qua email này.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildForgotPasswordOTPEmailBody(string userName, string otpCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF5722; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .otp-box {{ background-color: #fff3e0; border: 2px solid #FF5722; padding: 20px; border-radius: 10px; margin: 20px 0; text-align: center; }}
        .otp-code {{ font-size: 32px; font-weight: bold; color: #FF5722; letter-spacing: 5px; margin: 10px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Đặt lại mật khẩu</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
            <p>Vui lòng sử dụng mã OTP bên dưới để xác thực và đặt lại mật khẩu:</p>
            
            <div class='otp-box'>
                <h3>Mã OTP của bạn:</h3>
                <div class='otp-code'>{otpCode}</div>
                <p><strong>Mã này sẽ hết hạn sau 10 phút</strong></p>
            </div>
            
            <p><strong>Lưu ý bảo mật:</strong></p>
            <ul>
                <li>Không chia sẻ mã OTP này với bất kỳ ai</li>
                <li>Mã OTP chỉ có hiệu lực trong 10 phút</li>
                <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>
            </ul>
            
            <p>Nếu bạn gặp khó khăn, vui lòng liên hệ với bộ phận hỗ trợ khách hàng.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildPasswordChangedEmailBody(string userName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .success-badge {{ background-color: #4CAF50; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .security-info {{ background-color: #e8f5e8; border: 1px solid #4CAF50; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Mật khẩu đã được thay đổi</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <div class='success-badge'>Mật khẩu đã được thay đổi thành công!</div>
            <p>Chúng tôi xác nhận rằng mật khẩu tài khoản của bạn đã được thay đổi thành công.</p>
            
            <h3>Thông tin thay đổi:</h3>
            <p><strong>Thời gian:</strong> {DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm}</p>
            <p><strong>Trạng thái:</strong> Thành công</p>
            
            <div class='security-info'>
                <h4>🔒 Thông tin bảo mật:</h4>
                <p>Nếu bạn không thực hiện thay đổi này, vui lòng:</p>
                <ul>
                    <li>Liên hệ ngay với bộ phận hỗ trợ</li>
                    <li>Kiểm tra hoạt động đăng nhập gần đây</li>
                    <li>Thay đổi mật khẩu ngay lập tức</li>
                </ul>
            </div>
            
            <p>Để bảo vệ tài khoản của bạn, chúng tôi khuyên bạn nên:</p>
            <ul>
                <li>Sử dụng mật khẩu mạnh và duy nhất</li>
                <li>Không chia sẻ thông tin đăng nhập</li>
                <li>Đăng xuất khỏi các thiết bị công cộng</li>
            </ul>
            
            <p>Cảm ơn bạn đã sử dụng dịch vụ của MyApp!</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildAccountUpdatedEmailBody(string userName, string changes)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .update-badge {{ background-color: #2196F3; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .changes-box {{ background-color: #e3f2fd; border: 1px solid #2196F3; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📝 Thông tin tài khoản đã được cập nhật</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <div class='update-badge'>Thông tin tài khoản đã được cập nhật!</div>
            <p>Chúng tôi xác nhận rằng thông tin tài khoản của bạn đã được cập nhật thành công.</p>
            
            <h3>Thông tin cập nhật:</h3>
            <p><strong>Thời gian:</strong> {DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm}</p>
            <p><strong>Trạng thái:</strong> Thành công</p>
            
            <div class='changes-box'>
                <h4>Các thay đổi đã thực hiện:</h4>
                <p>{changes}</p>
            </div>
            
            <p>Nếu bạn không thực hiện các thay đổi này, vui lòng:</p>
            <ul>
                <li>Liên hệ ngay với bộ phận hỗ trợ khách hàng</li>
                <li>Kiểm tra hoạt động đăng nhập gần đây</li>
                <li>Thay đổi mật khẩu để bảo mật tài khoản</li>
            </ul>
            
            <p>Cảm ơn bạn đã sử dụng dịch vụ của MyApp!</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        // ========== TEMPLATE EMAIL MARKETING ==========

        private string BuildAbandonedCartEmailBody(string userName, List<OrderDetail> cartItems)
        {
            var itemsHtml = string.Join("", cartItems.Select(item => $@"
            <tr>
                <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.BoxType?.Name ?? "Unknown Product"}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{item.UnitPrice:N0} VNĐ</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{(item.Quantity * item.UnitPrice):N0} VNĐ</td>
            </tr>"));

            var totalAmount = cartItems.Sum(item => item.Quantity * item.UnitPrice);

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #E91E63; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .reminder-badge {{ background-color: #E91E63; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .cta-button {{ background-color: #E91E63; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th {{ background-color: #E91E63; color: white; padding: 10px; text-align: left; }}
        .total {{ font-size: 18px; font-weight: bold; color: #E91E63; text-align: right; padding: 10px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🛒 Bạn có sản phẩm chưa hoàn tất thanh toán</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <div class='reminder-badge'>Nhắc nhở thanh toán</div>
            <p>Chúng tôi nhận thấy bạn đã thêm sản phẩm vào giỏ hàng nhưng chưa hoàn tất thanh toán.</p>
            <p>Đừng bỏ lỡ cơ hội sở hữu những sản phẩm tuyệt vời này!</p>
            
            <h3>Sản phẩm trong giỏ hàng:</h3>
            <table>
                <thead>
                    <tr>
                        <th>Sản phẩm</th>
                        <th style='text-align: center;'>Số lượng</th>
                        <th style='text-align: right;'>Đơn giá</th>
                        <th style='text-align: right;'>Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>
            
            <div class='total'>
                Tổng cộng: {totalAmount:N0} VNĐ
            </div>
            
            <div style='text-align: center;'>
                <a href='https://myapp.com/cart' class='cta-button'>Hoàn tất thanh toán ngay</a>
            </div>
            
            <p><strong>Ưu đãi đặc biệt:</strong> Hoàn tất thanh toán trong 24 giờ tới để nhận được miễn phí vận chuyển!</p>
            
            <p>Nếu bạn gặp khó khăn trong quá trình thanh toán, vui lòng liên hệ với chúng tôi.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        // ========== TEMPLATE EMAIL HỆ THỐNG ==========

        private string BuildSystemMaintenanceEmailBody(string userName, DateTime maintenanceStart, DateTime maintenanceEnd)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #607D8B; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .maintenance-badge {{ background-color: #607D8B; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .time-box {{ background-color: #eceff1; border: 1px solid #607D8B; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔧 Thông báo bảo trì hệ thống</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <div class='maintenance-badge'>Bảo trì hệ thống</div>
            <p>Chúng tôi sẽ thực hiện bảo trì hệ thống để cải thiện dịch vụ và trải nghiệm người dùng.</p>
            
            <div class='time-box'>
                <h3>⏰ Thời gian bảo trì:</h3>
                <p><strong>Bắt đầu:</strong> {maintenanceStart:dd/MM/yyyy HH:mm}</p>
                <p><strong>Kết thúc:</strong> {maintenanceEnd:dd/MM/yyyy HH:mm}</p>
                <p><strong>Thời gian dự kiến:</strong> {(maintenanceEnd - maintenanceStart).TotalHours:F1} giờ</p>
            </div>
            
            <h3>📋 Những gì sẽ bị ảnh hưởng:</h3>
            <ul>
                <li>Website có thể tạm thời không truy cập được</li>
                <li>Ứng dụng di động có thể gặp sự cố</li>
                <li>Email thông báo có thể bị trễ</li>
                <li>Thanh toán trực tuyến có thể tạm ngưng</li>
            </ul>
            
            <h3>✅ Sau khi bảo trì:</h3>
            <ul>
                <li>Hiệu suất hệ thống được cải thiện</li>
                <li>Tính năng mới được thêm vào</li>
                <li>Bảo mật được nâng cấp</li>
                <li>Trải nghiệm người dùng tốt hơn</li>
            </ul>
            
            <p>Chúng tôi xin lỗi vì sự bất tiện này và cảm ơn sự kiên nhẫn của bạn.</p>
            <p>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ với bộ phận hỗ trợ khách hàng.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        // ========== TEMPLATE EMAIL CẢNH BÁO CHO ADMIN ==========

        private string BuildHighValueOrderAlertBody(Order order, decimal threshold, string userName, string maskedUserId, string phone, string address, string shortOrderId)
        {
            var itemsHtml = string.Join("", order.OrderDetails.Select(item => $@"
            <tr>
                <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.BoxType?.Name ?? "Unknown Product"}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{item.UnitPrice:N0} VNĐ</td>
                <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>{(item.Quantity * item.UnitPrice):N0} VNĐ</td>
            </tr>"));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert-badge {{ background-color: #f44336; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .amount-box {{ background-color: #ffebee; border: 2px solid #f44336; padding: 15px; border-radius: 5px; margin: 20px 0; text-align: center; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th {{ background-color: #f44336; color: white; padding: 10px; text-align: left; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚨 Đơn hàng giá trị cao</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <div class='alert-badge'>CẢNH BÁO: Đơn hàng vượt ngưỡng</div>
            <p>Đơn hàng này có giá trị vượt quá ngưỡng cảnh báo đã thiết lập.</p>
            
            <div class='amount-box'>
                <h3>Giá trị đơn hàng:</h3>
                <h2 style='color: #f44336; margin: 0;'>{order.FinalPrice:N0} VNĐ</h2>
                <p>Ngưỡng cảnh báo: {threshold:N0} VNĐ</p>
            </div>
            
            <h3>Thông tin khách hàng:</h3>
            <p><strong>User ID:</strong> {maskedUserId}</p>
            <p><strong>Tên:</strong> {userName}</p>
            <p><strong>Số điện thoại:</strong> {phone}</p>
            <p><strong>Địa chỉ:</strong> {address}</p>
            <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
            <p><strong>Trạng thái:</strong> {order.Status}</p>
            
            <h3>Chi tiết sản phẩm:</h3>
            <table>
                <thead>
                    <tr>
                        <th>Sản phẩm</th>
                        <th style='text-align: center;'>Số lượng</th>
                        <th style='text-align: right;'>Đơn giá</th>
                        <th style='text-align: right;'>Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>
            
            <p><strong>Hành động đề xuất:</strong></p>
            <ul>
                <li>Kiểm tra thông tin khách hàng</li>
                <li>Xác minh phương thức thanh toán</li>
                <li>Theo dõi quá trình xử lý đơn hàng</li>
                <li>Liên hệ khách hàng nếu cần thiết</li>
            </ul>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp - Admin Alert System</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildOrderCancelledAlertBody(Order order, string reason, string userName, string maskedUserId, string phone, string address, string shortOrderId)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert-badge {{ background-color: #FF9800; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .reason-box {{ background-color: #fff3e0; border: 1px solid #FF9800; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Đơn hàng bị hủy</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <div class='alert-badge'>CẢNH BÁO: Đơn hàng đã bị hủy</div>
            <p>Đơn hàng này đã bị hủy và cần được xem xét.</p>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>User ID:</strong> {maskedUserId}</p>
            <p><strong>Tên:</strong> {userName}</p>
            <p><strong>Số điện thoại:</strong> {phone}</p>
            <p><strong>Địa chỉ:</strong> {address}</p>
            <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Ngày hủy:</strong> {DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm}</p>
            <p><strong>Giá trị:</strong> {order.FinalPrice:N0} VNĐ</p>
            <p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
            
            <div class='reason-box'>
                <h4>Lý do hủy đơn hàng:</h4>
                <p>{reason}</p>
            </div>
            
            <p><strong>Hành động đề xuất:</strong></p>
            <ul>
                <li>Kiểm tra lý do hủy đơn hàng</li>
                <li>Xử lý hoàn tiền nếu cần thiết</li>
                <li>Cập nhật tồn kho sản phẩm</li>
                <li>Liên hệ khách hàng để tìm hiểu nguyên nhân</li>
            </ul>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp - Admin Alert System</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildPendingOrderAlertBody(Order order, TimeSpan pendingTime, string userName, string maskedUserId, string phone, string address, string shortOrderId)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FFC107; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert-badge {{ background-color: #FFC107; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .time-box {{ background-color: #fff8e1; border: 1px solid #FFC107; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⏰ Đơn hàng chờ xác nhận quá lâu</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <div class='alert-badge'>CẢNH BÁO: Đơn hàng chờ xử lý quá lâu</div>
            <p>Đơn hàng này đã chờ xác nhận quá lâu và cần được xử lý ngay.</p>
            
            <div class='time-box'>
                <h3>Thời gian chờ:</h3>
                <h2 style='color: #FFC107; margin: 0;'>{pendingTime.TotalHours:F1} giờ</h2>
                <p>Đơn hàng đã chờ xử lý quá lâu</p>
            </div>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>User ID:</strong> {maskedUserId}</p>
            <p><strong>Tên:</strong> {userName}</p>
            <p><strong>Số điện thoại:</strong> {phone}</p>
            <p><strong>Địa chỉ:</strong> {address}</p>
            <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Trạng thái:</strong> {order.Status}</p>
            <p><strong>Giá trị:</strong> {order.FinalPrice:N0} VNĐ</p>
            <p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
            
            <p><strong>Hành động đề xuất:</strong></p>
            <ul>
                <li>Xử lý đơn hàng ngay lập tức</li>
                <li>Liên hệ khách hàng để xác nhận</li>
                <li>Kiểm tra tình trạng thanh toán</li>
                <li>Cập nhật trạng thái đơn hàng</li>
            </ul>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp - Admin Alert System</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildPaymentFailedAlertBody(Order order, int failureCount, string userName, string maskedUserId, string phone, string address, string shortOrderId)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert-badge {{ background-color: #f44336; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .failure-box {{ background-color: #ffebee; border: 2px solid #f44336; padding: 15px; border-radius: 5px; margin: 20px 0; text-align: center; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💳 Thanh toán thất bại nhiều lần</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <div class='alert-badge'>CẢNH BÁO: Nghi ngờ gian lận</div>
            <p>Đơn hàng này có nhiều lần thanh toán thất bại và cần được xem xét.</p>
            
            <div class='failure-box'>
                <h3>Số lần thanh toán thất bại:</h3>
                <h2 style='color: #f44336; margin: 0;'>{failureCount} lần</h2>
                <p>Có thể có vấn đề về bảo mật</p>
            </div>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>User ID:</strong> {maskedUserId}</p>
            <p><strong>Tên:</strong> {userName}</p>
            <p><strong>Số điện thoại:</strong> {phone}</p>
            <p><strong>Địa chỉ:</strong> {address}</p>
            <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Trạng thái:</strong> {order.Status}</p>
            <p><strong>Giá trị:</strong> {order.FinalPrice:N0} VNĐ</p>
            <p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
            
            <p><strong>Hành động đề xuất:</strong></p>
            <ul>
                <li>Kiểm tra thông tin khách hàng</li>
                <li>Xác minh phương thức thanh toán</li>
                <li>Liên hệ khách hàng để xác nhận</li>
                <li>Tạm thời khóa tài khoản nếu cần thiết</li>
                <li>Báo cáo cho bộ phận bảo mật</li>
            </ul>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp - Admin Alert System</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildRefundRequestAlertBody(Order order, string reason, string userName, string maskedUserId, string phone, string address, string shortOrderId)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert-badge {{ background-color: #2196F3; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .reason-box {{ background-color: #e3f2fd; border: 1px solid #2196F3; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💰 Yêu cầu hoàn tiền</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <div class='alert-badge'>CẢNH BÁO: Yêu cầu hoàn tiền cần phê duyệt</div>
            <p>Khách hàng đã yêu cầu hoàn tiền cho đơn hàng này.</p>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>User ID:</strong> {maskedUserId}</p>
            <p><strong>Tên:</strong> {userName}</p>
            <p><strong>Số điện thoại:</strong> {phone}</p>
            <p><strong>Địa chỉ:</strong> {address}</p>
            <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Trạng thái:</strong> {order.Status}</p>
            <p><strong>Giá trị:</strong> {order.FinalPrice:N0} VNĐ</p>
            <p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
            
            <div class='reason-box'>
                <h4>Lý do yêu cầu hoàn tiền:</h4>
                <p>{reason}</p>
            </div>
            
            <p><strong>Hành động đề xuất:</strong></p>
            <ul>
                <li>Xem xét lý do hoàn tiền</li>
                <li>Kiểm tra tình trạng sản phẩm</li>
                <li>Liên hệ khách hàng để xác nhận</li>
                <li>Phê duyệt hoặc từ chối yêu cầu</li>
                <li>Xử lý hoàn tiền nếu được phê duyệt</li>
            </ul>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp - Admin Alert System</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildDeliveryIssueAlertBody(Order order, string issue, string userName, string maskedUserId, string phone, string address, string shortOrderId)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #9C27B0; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert-badge {{ background-color: #9C27B0; color: white; padding: 10px 20px; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .issue-box {{ background-color: #f3e5f5; border: 1px solid #9C27B0; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚚 Đơn hàng có vấn đề giao hàng</h1>
            <p>Đơn hàng #{shortOrderId}</p>
        </div>
        <div class='content'>
            <div class='alert-badge'>CẢNH BÁO: Vấn đề giao hàng</div>
            <p>Đơn hàng này gặp vấn đề trong quá trình giao hàng.</p>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{shortOrderId}</p>
            <p><strong>User ID:</strong> {maskedUserId}</p>
            <p><strong>Tên:</strong> {userName}</p>
            <p><strong>Số điện thoại:</strong> {phone}</p>
            <p><strong>Địa chỉ:</strong> {address}</p>
            <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
            <p><strong>Trạng thái:</strong> {order.Status}</p>
            <p><strong>Giá trị:</strong> {order.FinalPrice:N0} VNĐ</p>
            <p><strong>Phương thức giao hàng:</strong> {order.DeliveryMethod}</p>
            
            <div class='issue-box'>
                <h4>Vấn đề giao hàng:</h4>
                <p>{issue}</p>
            </div>
            
            <p><strong>Hành động đề xuất:</strong></p>
            <ul>
                <li>Liên hệ đơn vị vận chuyển</li>
                <li>Thông báo cho khách hàng</li>
                <li>Tìm giải pháp thay thế</li>
                <li>Cập nhật trạng thái giao hàng</li>
                <li>Xử lý khiếu nại nếu có</li>
            </ul>
        </div>
        <div class='footer'>
            <p>&copy; 2025 MyApp - Admin Alert System</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
