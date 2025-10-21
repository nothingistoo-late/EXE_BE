using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Commons;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implements
{
    public class GiftBoxOrderRepository : GenericRepository<GiftBoxOrder, Guid>, IGiftBoxOrderRepository
    {
        private new readonly EXE_BE _context;

        public GiftBoxOrderRepository(EXE_BE context) : base(context)
        {
            _context = context;
        }

        public async Task<List<GiftBoxOrder>> GetByOrderIdAsync(Guid orderId)
        {
            var result = await GetAllAsync(
                gbo => gbo.OrderId == orderId && !gbo.IsDeleted,
                query => query.OrderByDescending(gbo => gbo.CreatedAt),
                gbo => gbo.Order);
            return result.ToList();
        }

        public async Task<GiftBoxOrder?> GetByOrderIdAndGiftBoxOrderIdAsync(Guid orderId, Guid giftBoxOrderId)
        {
            return await FirstOrDefaultAsync(
                gbo => gbo.Id == giftBoxOrderId && gbo.OrderId == orderId && !gbo.IsDeleted,
                gbo => gbo.Order);
        }
    }
}
