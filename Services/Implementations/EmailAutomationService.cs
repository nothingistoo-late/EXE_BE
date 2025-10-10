using Services.Interfaces;
using Services.Commons.Gmail;
using Repositories.Interfaces;
using BusinessObjects;
using BusinessObjects.Common;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class EmailAutomationService : IEmailAutomationService
    {
        private readonly IEXEGmailService _emailService;
        private readonly IGenericRepository<Order, Guid> _orderRepository;
        private readonly IGenericRepository<OrderDetail, Guid> _orderDetailRepository;
        private readonly ILogger<EmailAutomationService> _logger;
        private readonly ICurrentTime _currentTime;

        public EmailAutomationService(
            IEXEGmailService emailService,
            IGenericRepository<Order, Guid> orderRepository,
            IGenericRepository<OrderDetail, Guid> orderDetailRepository,
            ILogger<EmailAutomationService> logger,
            ICurrentTime currentTime)
        {
            _emailService = emailService;
            _orderRepository = orderRepository;
            _orderDetailRepository = orderDetailRepository;
            _logger = logger;
            _currentTime = currentTime;
        }

        // ========== EMAIL TỰ ĐỘNG CHO ĐƠN HÀNG ==========

        public async Task SendOrderCreatedEmailAsync(Order order, string userEmail)
        {
            try
            {
                await _emailService.SendOrderConfirmationEmailAsync(userEmail, order);
                _logger.LogInformation($"Đã gửi email xác nhận đơn hàng cho đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email xác nhận đơn hàng cho đơn hàng {order.Id}");
            }
        }

        public async Task SendPaymentSuccessEmailAsync(Order order, string userEmail)
        {
            try
            {
                await _emailService.SendPaymentSuccessEmailAsync(userEmail, order);
                _logger.LogInformation($"Đã gửi email thanh toán thành công cho đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email thanh toán thành công cho đơn hàng {order.Id}");
            }
        }

        public async Task SendOrderPreparationEmailAsync(Order order, string userEmail)
        {
            try
            {
                await _emailService.SendOrderPreparationEmailAsync(userEmail, order);
                _logger.LogInformation($"Đã gửi email đơn hàng đang chuẩn bị cho đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email đơn hàng đang chuẩn bị cho đơn hàng {order.Id}");
            }
        }

        public async Task SendOrderDeliveredEmailAsync(Order order, string userEmail)
        {
            try
            {
                await _emailService.SendOrderDeliveredEmailAsync(userEmail, order);
                _logger.LogInformation($"Đã gửi email giao hàng thành công cho đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email giao hàng thành công cho đơn hàng {order.Id}");
            }
        }

        public async Task SendOrderCancelledEmailAsync(Order order, string userEmail, string reason)
        {
            try
            {
                await _emailService.SendOrderCancelledEmailAsync(userEmail, order, reason);
                _logger.LogInformation($"Đã gửi email hủy đơn hàng cho đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email hủy đơn hàng cho đơn hàng {order.Id}");
            }
        }

        public async Task SendRefundProcessedEmailAsync(Order order, string userEmail, decimal refundAmount)
        {
            try
            {
                await _emailService.SendRefundProcessedEmailAsync(userEmail, order, refundAmount);
                _logger.LogInformation($"Đã gửi email hoàn tiền cho đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email hoàn tiền cho đơn hàng {order.Id}");
            }
        }

        // ========== EMAIL TỰ ĐỘNG CHO TÀI KHOẢN ==========

        public async Task SendRegistrationSuccessEmailAsync(string userEmail, string userName)
        {
            try
            {
                await _emailService.SendRegistrationSuccessEmailAsync(userEmail, userName);
                _logger.LogInformation($"Đã gửi email đăng ký thành công cho user {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email đăng ký thành công cho user {userEmail}");
            }
        }

        public async Task SendEmailVerificationEmailAsync(string userEmail, string userName, string verificationLink)
        {
            try
            {
                await _emailService.SendEmailVerificationEmailAsync(userEmail, userName, verificationLink);
                _logger.LogInformation($"Đã gửi email xác thực cho user {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email xác thực cho user {userEmail}");
            }
        }

        public async Task SendPasswordChangedEmailAsync(string userEmail, string userName)
        {
            try
            {
                await _emailService.SendPasswordChangedEmailAsync(userEmail, userName);
                _logger.LogInformation($"Đã gửi email thay đổi mật khẩu cho user {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email thay đổi mật khẩu cho user {userEmail}");
            }
        }

        public async Task SendAccountUpdatedEmailAsync(string userEmail, string userName, string changes)
        {
            try
            {
                await _emailService.SendAccountUpdatedEmailAsync(userEmail, userName, changes);
                _logger.LogInformation($"Đã gửi email cập nhật tài khoản cho user {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email cập nhật tài khoản cho user {userEmail}");
            }
        }

        // ========== EMAIL TỰ ĐỘNG MARKETING ==========

        public async Task SendAbandonedCartEmailAsync(string userEmail, string userName, List<OrderDetail> cartItems)
        {
            try
            {
                await _emailService.SendAbandonedCartEmailAsync(userEmail, userName, cartItems);
                _logger.LogInformation($"Đã gửi email abandoned cart cho user {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email abandoned cart cho user {userEmail}");
            }
        }

        // ========== EMAIL TỰ ĐỘNG CẢNH BÁO ADMIN ==========

        public async Task SendNewOrderAlertToAdminAsync(Order order)
        {
            try
            {
                await _emailService.SendNewOrderNotificationToAdminAsync(order);
                _logger.LogInformation($"Đã gửi email cảnh báo đơn hàng mới cho admin - đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email cảnh báo đơn hàng mới cho admin - đơn hàng {order.Id}");
            }
        }

        public async Task SendHighValueOrderAlertToAdminAsync(Order order, decimal threshold = 10000000)
        {
            try
            {
                if (order.FinalPrice > (double)threshold)
                {
                    await _emailService.SendHighValueOrderAlertAsync(order, threshold);
                    _logger.LogInformation($"Đã gửi email cảnh báo đơn hàng giá trị cao cho admin - đơn hàng {order.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email cảnh báo đơn hàng giá trị cao cho admin - đơn hàng {order.Id}");
            }
        }

        public async Task SendOrderCancelledAlertToAdminAsync(Order order, string reason)
        {
            try
            {
                await _emailService.SendOrderCancelledAlertAsync(order, reason);
                _logger.LogInformation($"Đã gửi email cảnh báo đơn hàng bị hủy cho admin - đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email cảnh báo đơn hàng bị hủy cho admin - đơn hàng {order.Id}");
            }
        }

        public async Task SendPendingOrderAlertToAdminAsync(Order order, TimeSpan pendingTime)
        {
            try
            {
                await _emailService.SendPendingOrderAlertAsync(order, pendingTime);
                _logger.LogInformation($"Đã gửi email cảnh báo đơn hàng chờ xác nhận cho admin - đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email cảnh báo đơn hàng chờ xác nhận cho admin - đơn hàng {order.Id}");
            }
        }

        public async Task SendPaymentFailedAlertToAdminAsync(Order order, int failureCount)
        {
            try
            {
                await _emailService.SendPaymentFailedAlertAsync(order, failureCount);
                _logger.LogInformation($"Đã gửi email cảnh báo thanh toán thất bại cho admin - đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email cảnh báo thanh toán thất bại cho admin - đơn hàng {order.Id}");
            }
        }

        public async Task SendRefundRequestAlertToAdminAsync(Order order, string reason)
        {
            try
            {
                await _emailService.SendRefundRequestAlertAsync(order, reason);
                _logger.LogInformation($"Đã gửi email cảnh báo yêu cầu hoàn tiền cho admin - đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email cảnh báo yêu cầu hoàn tiền cho admin - đơn hàng {order.Id}");
            }
        }

        public async Task SendDeliveryIssueAlertToAdminAsync(Order order, string issue)
        {
            try
            {
                await _emailService.SendDeliveryIssueAlertAsync(order, issue);
                _logger.LogInformation($"Đã gửi email cảnh báo vấn đề giao hàng cho admin - đơn hàng {order.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email cảnh báo vấn đề giao hàng cho admin - đơn hàng {order.Id}");
            }
        }

        // ========== EMAIL TỰ ĐỘNG HỆ THỐNG ==========

        public async Task SendSystemMaintenanceEmailAsync(string userEmail, string userName, DateTime maintenanceStart, DateTime maintenanceEnd)
        {
            try
            {
                await _emailService.SendSystemMaintenanceEmailAsync(userEmail, userName, maintenanceStart, maintenanceEnd);
                _logger.LogInformation($"Đã gửi email thông báo bảo trì hệ thống cho user {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi gửi email thông báo bảo trì hệ thống cho user {userEmail}");
            }
        }

        // ========== UTILITY METHODS ==========

        public async Task CheckAndSendPendingOrderAlertsAsync()
        {
            try
            {
                // Lấy các đơn hàng chờ xác nhận quá 2 giờ
                var pendingOrders = await _orderRepository.GetAllAsync();
                var overdueOrders = pendingOrders.Where(o => 
                    o.Status == OrderStatus.Pending && 
                    _currentTime.GetVietnamTime() - o.CreatedAt > TimeSpan.FromHours(2)
                ).ToList();

                foreach (var order in overdueOrders)
                {
                    var pendingTime = _currentTime.GetVietnamTime() - order.CreatedAt;
                    await SendPendingOrderAlertToAdminAsync(order, pendingTime);
                }

                _logger.LogInformation($"Đã kiểm tra và gửi {overdueOrders.Count} email cảnh báo đơn hàng chờ xác nhận");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kiểm tra và gửi email cảnh báo đơn hàng chờ xác nhận");
            }
        }

        public async Task CheckAndSendAbandonedCartEmailsAsync()
        {
            try
            {
                // Logic để kiểm tra và gửi email abandoned cart
                // Có thể implement dựa trên session timeout hoặc cart activity
                _logger.LogInformation("Đã kiểm tra và gửi email abandoned cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kiểm tra và gửi email abandoned cart");
            }
        }
    }
}
