using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects;

namespace Services.Commons.Gmail
{
    public interface IEXEGmailService
    {
        // Email liên quan đến đơn hàng
        Task SendOrderConfirmationEmailAsync(string toEmail, Order order);
        Task SendPaymentSuccessEmailAsync(string toEmail, Order order);
        Task SendOrderPreparationEmailAsync(string toEmail, Order order);
        Task SendOrderDeliveredEmailAsync(string toEmail, Order order);
        Task SendOrderCancelledEmailAsync(string toEmail, Order order, string reason);
        Task SendRefundProcessedEmailAsync(string toEmail, Order order, decimal refundAmount);

        // Email liên quan đến tài khoản
        Task SendRegistrationSuccessEmailAsync(string toEmail, string userName);
        Task SendEmailVerificationEmailAsync(string toEmail, string userName, string verificationLink);
        Task SendForgotPasswordOTPEmailAsync(string toEmail, string userName, string otpCode);
        Task SendPasswordChangedEmailAsync(string toEmail, string userName);
        Task SendAccountUpdatedEmailAsync(string toEmail, string userName, string changes);

        // Email marketing & chăm sóc khách hàng
        Task SendAbandonedCartEmailAsync(string toEmail, string userName, List<OrderDetail> cartItems);

        // Email hệ thống
        Task SendSystemMaintenanceEmailAsync(string toEmail, string userName, DateTime maintenanceStart, DateTime maintenanceEnd);

        // Cảnh báo về đơn hàng (gửi cho admin)
        Task SendNewOrderNotificationToAdminAsync(Order order);
        Task SendHighValueOrderAlertAsync(Order order, decimal threshold);
        Task SendOrderCancelledAlertAsync(Order order, string reason);
        Task SendPendingOrderAlertAsync(Order order, TimeSpan pendingTime);
        Task SendPaymentFailedAlertAsync(Order order, int failureCount);
        Task SendRefundRequestAlertAsync(Order order, string reason);
        Task SendDeliveryIssueAlertAsync(Order order, string issue);

        // Utility
        Task SendCustomEmailAsync(string toEmail, string subject, string body);
    }
}
