using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects;
using Repositories.Interfaces;

namespace Services.Commons.Gmail
{
    public class EXEGmailService : IEXEGmailService
    {
        private IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly IUnitOfWork _unitOfWork;

        public EXEGmailService(IUnitOfWork unitOfWork, IOptions<EmailSettings> emailSettings,IEmailService emailService)
        {
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
            _unitOfWork = unitOfWork;

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
            var subject = $"Order Confirmation - #{order.Id}";
            var body = BuildOrderConfirmationEmailBody(order);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPaymentSuccessEmailAsync(string toEmail, Order order)
        {
            var subject = $"Payment Successful - Order #{order.Id}";
            var body = BuildPaymentSuccessEmailBody(order);
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendNewOrderNotificationToAdminAsync(Order order)
        {
            var subject = $"New Order Received - #{order.Id}";
            var body = BuildNewOrderNotificationBody(order);
            await _emailService.SendEmailAsync(_emailSettings.AdminEmail, subject, body);
        }

        public async Task SendCustomEmailAsync(string toEmail, string subject, string body)
        {
            await _emailService.SendEmailAsync(toEmail, subject, body);
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

        private string BuildOrderConfirmationEmailBody(Order order)
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
            <h1>Order Confirmation</h1>
            <p>Order #{order.Id}</p>
        </div>
        <div class='content'>
            <h2>Xin chào khách hàng,</h2>
            <p>Cảm ơn bạn đã đặt hàng! Đơn hàng của bạn đã được xác nhận.</p>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{order.Id}</p>
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

        private string BuildPaymentSuccessEmailBody(Order order)
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
            <h1>✓ Payment Successful</h1>
            <p>Order #{order.Id}</p>
        </div>
        <div class='content'>
            <h2>Xin chào khách hàng,</h2>
            <div class='success-badge'>Thanh toán thành công!</div>
            <p>Chúng tôi đã nhận được thanh toán của bạn. Đơn hàng đang được xử lý.</p>
            
            <h3>Thông tin thanh toán:</h3>
            <p><strong>Mã đơn hàng:</strong> #{order.Id}</p>
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

        private string BuildNewOrderNotificationBody(Order order)
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
            <h1>🔔 New Order Alert</h1>
            <p>Order #{order.Id}</p>
        </div>
        <div class='content'>
            <div class='alert'>
                <strong>⚠️ Có đơn hàng mới cần xử lý!</strong>
            </div>
            
            <h3>Thông tin khách hàng:</h3>
            <p><strong>User ID:</strong> {order.UserId}</p>
            <p><strong>Trạng thái:</strong> {order.Status}</p>
            <p><strong>Đã thanh toán:</strong> {(order.IsPaid ? "Có" : "Chưa")}</p>
            
            <h3>Thông tin đơn hàng:</h3>
            <p><strong>Mã đơn hàng:</strong> #{order.Id}</p>
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
    }
}
