using System;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IPendingOrderTrackingService
    {
        void TrackPendingOrder(Guid orderId);
        void RemovePendingOrder(Guid orderId);
        Task CheckAndSendPendingAlertsAsync();
        TimeSpan? GetPendingTime(Guid orderId);
    }
}
