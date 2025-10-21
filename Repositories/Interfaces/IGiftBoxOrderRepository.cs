using BusinessObjects;
using Repositories.Commons;

namespace Repositories.Interfaces
{
    public interface IGiftBoxOrderRepository : IGenericRepository<GiftBoxOrder, Guid>
    {
        Task<List<GiftBoxOrder>> GetByOrderIdAsync(Guid orderId);
        Task<GiftBoxOrder?> GetByOrderIdAndGiftBoxOrderIdAsync(Guid orderId, Guid giftBoxOrderId);
    }
}
