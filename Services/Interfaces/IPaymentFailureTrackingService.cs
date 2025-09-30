using BusinessObjects;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IPaymentFailureTrackingService
    {
        Task RecordPaymentFailureAsync(Guid orderId, Order order);
        void ClearFailureCount(Guid orderId);
        int GetFailureCount(Guid orderId);
    }
}
