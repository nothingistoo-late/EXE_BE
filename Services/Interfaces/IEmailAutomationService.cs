using BusinessObjects;

namespace Services.Interfaces
{
    public interface IEmailAutomationService
    {
        // ========== EMAIL TỰ ĐỘNG CHO ĐƠN HÀNG ==========
        
        /// <summary>
        /// Tự động gửi email khi tạo đơn hàng thành công
        /// </summary>
        Task SendOrderCreatedEmailAsync(Order order, string userEmail);

        /// <summary>
        /// Tự động gửi email khi thanh toán thành công
        /// </summary>
        Task SendPaymentSuccessEmailAsync(Order order, string userEmail);

        /// <summary>
        /// Tự động gửi email khi đơn hàng chuyển sang trạng thái "Đang chuẩn bị"
        /// </summary>
        Task SendOrderPreparationEmailAsync(Order order, string userEmail);

        /// <summary>
        /// Tự động gửi email khi đơn hàng chuyển sang trạng thái "Đã giao hàng"
        /// </summary>
        Task SendOrderDeliveredEmailAsync(Order order, string userEmail);

        /// <summary>
        /// Tự động gửi email khi đơn hàng bị hủy
        /// </summary>
        Task SendOrderCancelledEmailAsync(Order order, string userEmail, string reason);

        /// <summary>
        /// Tự động gửi email khi hoàn tiền
        /// </summary>
        Task SendRefundProcessedEmailAsync(Order order, string userEmail, decimal refundAmount);

        // ========== EMAIL TỰ ĐỘNG CHO TÀI KHOẢN ==========

        /// <summary>
        /// Tự động gửi email khi đăng ký tài khoản thành công
        /// </summary>
        Task SendRegistrationSuccessEmailAsync(string userEmail, string userName);

        /// <summary>
        /// Tự động gửi email xác thực khi đăng ký
        /// </summary>
        Task SendEmailVerificationEmailAsync(string userEmail, string userName, string verificationLink);

        /// <summary>
        /// Tự động gửi email khi thay đổi mật khẩu thành công
        /// </summary>
        Task SendPasswordChangedEmailAsync(string userEmail, string userName);

        /// <summary>
        /// Tự động gửi email khi cập nhật thông tin tài khoản
        /// </summary>
        Task SendAccountUpdatedEmailAsync(string userEmail, string userName, string changes);

        // ========== EMAIL TỰ ĐỘNG MARKETING ==========

        /// <summary>
        /// Tự động gửi email abandoned cart sau 1 giờ không hoạt động
        /// </summary>
        Task SendAbandonedCartEmailAsync(string userEmail, string userName, List<OrderDetail> cartItems);

        // ========== EMAIL TỰ ĐỘNG CẢNH BÁO ADMIN ==========

        /// <summary>
        /// Tự động gửi email cảnh báo admin khi có đơn hàng mới
        /// </summary>
        Task SendNewOrderAlertToAdminAsync(Order order);

        /// <summary>
        /// Tự động gửi email cảnh báo admin khi đơn hàng có giá trị cao
        /// </summary>
        Task SendHighValueOrderAlertToAdminAsync(Order order, decimal threshold = 10000000);

        /// <summary>
        /// Tự động gửi email cảnh báo admin khi đơn hàng bị hủy
        /// </summary>
        Task SendOrderCancelledAlertToAdminAsync(Order order, string reason);

        /// <summary>
        /// Tự động gửi email cảnh báo admin khi đơn hàng chờ xác nhận quá lâu
        /// </summary>
        Task SendPendingOrderAlertToAdminAsync(Order order, TimeSpan pendingTime);

        /// <summary>
        /// Tự động gửi email cảnh báo admin khi thanh toán thất bại nhiều lần
        /// </summary>
        Task SendPaymentFailedAlertToAdminAsync(Order order, int failureCount);

        /// <summary>
        /// Tự động gửi email cảnh báo admin khi có yêu cầu hoàn tiền
        /// </summary>
        Task SendRefundRequestAlertToAdminAsync(Order order, string reason);

        /// <summary>
        /// Tự động gửi email cảnh báo admin khi có vấn đề giao hàng
        /// </summary>
        Task SendDeliveryIssueAlertToAdminAsync(Order order, string issue);

        // ========== EMAIL TỰ ĐỘNG HỆ THỐNG ==========

        /// <summary>
        /// Tự động gửi email thông báo bảo trì hệ thống
        /// </summary>
        Task SendSystemMaintenanceEmailAsync(string userEmail, string userName, DateTime maintenanceStart, DateTime maintenanceEnd);

        // ========== UTILITY METHODS ==========

        /// <summary>
        /// Kiểm tra và gửi email cảnh báo cho đơn hàng chờ xác nhận quá lâu
        /// </summary>
        Task CheckAndSendPendingOrderAlertsAsync();

        /// <summary>
        /// Kiểm tra và gửi email abandoned cart
        /// </summary>
        Task CheckAndSendAbandonedCartEmailsAsync();
    }
}
