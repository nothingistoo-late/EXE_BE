using BusinessObjects;
using Services.Commons.Gmail;
using Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class PendingOrderTrackingService : IPendingOrderTrackingService
    {
        private readonly IEXEGmailService _emailService;
        private static readonly ConcurrentDictionary<Guid, DateTime> _pendingOrders = new ConcurrentDictionary<Guid, DateTime>();
        private static readonly ConcurrentDictionary<Guid, bool> _alertedOrders = new ConcurrentDictionary<Guid, bool>();
        private static readonly TimeSpan PENDING_THRESHOLD = TimeSpan.FromHours(24); // Ngưỡng 24 giờ

        public PendingOrderTrackingService(IEXEGmailService emailService)
        {
            _emailService = emailService;
        }

        public void TrackPendingOrder(Guid orderId)
        {
            _pendingOrders.AddOrUpdate(orderId, DateTime.UtcNow, (key, value) => value);
        }

        public void RemovePendingOrder(Guid orderId)
        {
            _pendingOrders.TryRemove(orderId, out _);
            _alertedOrders.TryRemove(orderId, out _); // Xóa cả trạng thái đã cảnh báo
        }

        public async Task CheckAndSendPendingAlertsAsync()
        {
            var now = DateTime.UtcNow;
            var ordersToAlert = new List<(Guid orderId, TimeSpan pendingTime)>();

            foreach (var kvp in _pendingOrders)
            {
                var pendingTime = now - kvp.Value;
                // Chỉ gửi cảnh báo nếu chưa gửi trước đó
                if (pendingTime >= PENDING_THRESHOLD && !_alertedOrders.ContainsKey(kvp.Key))
                {
                    ordersToAlert.Add((kvp.Key, pendingTime));
                }
            }

            // Gửi cảnh báo cho các đơn hàng chờ quá lâu
            foreach (var (orderId, pendingTime) in ordersToAlert)
            {
                try
                {
                    // Lấy thông tin đơn hàng (cần inject repository hoặc service)
                    // Tạm thời tạo order giả để test
                    var order = new Order
                    {
                        Id = orderId,
                        Status = OrderStatus.Pending,
                        CreatedAt = now - pendingTime
                    };

                    await _emailService.SendPendingOrderAlertAsync(order, pendingTime);
                    
                    // Đánh dấu đã gửi cảnh báo cho đơn hàng này
                    _alertedOrders.TryAdd(orderId, true);
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không dừng process
                    Console.WriteLine($"Error sending pending alert for order {orderId}: {ex.Message}");
                }
            }
        }

        public TimeSpan? GetPendingTime(Guid orderId)
        {
            if (_pendingOrders.TryGetValue(orderId, out var startTime))
            {
                return DateTime.UtcNow - startTime;
            }
            return null;
        }
    }
}
