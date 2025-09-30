using Services.Commons.Gmail;
using Services.Interfaces;
using System.Collections.Concurrent;

namespace Services.Implementations
{
    public class PaymentFailureTrackingService : IPaymentFailureTrackingService
    {
        private readonly IEXEGmailService _emailService;
        private static readonly ConcurrentDictionary<Guid, int> _failureCounts = new ConcurrentDictionary<Guid, int>();
        private const int MAX_FAILURE_COUNT = 3; // Ngưỡng cảnh báo

        public PaymentFailureTrackingService(IEXEGmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task RecordPaymentFailureAsync(Guid orderId, BusinessObjects.Order order)
        {
            var currentCount = _failureCounts.AddOrUpdate(orderId, 1, (key, value) => value + 1);
            
            // Gửi cảnh báo khi vượt ngưỡng
            if (currentCount >= MAX_FAILURE_COUNT)
            {
                await _emailService.SendPaymentFailedAlertAsync(order, currentCount);
            }
        }

        public void ClearFailureCount(Guid orderId)
        {
            _failureCounts.TryRemove(orderId, out _);
        }

        public int GetFailureCount(Guid orderId)
        {
            return _failureCounts.GetOrAdd(orderId, 0);
        }
    }
}
