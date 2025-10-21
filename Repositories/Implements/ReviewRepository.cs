using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Commons;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implements
{
    public class ReviewRepository : GenericRepository<Review, Guid>, IReviewRepository
    {
        private new readonly EXE_BE _context;

        public ReviewRepository(EXE_BE context) : base(context)
        {
            _context = context;
        }

        public async Task<Review?> GetByGiftBoxOrderIdAsync(Guid giftBoxOrderId)
        {
            return await FirstOrDefaultAsync(
                r => r.GiftBoxOrderId == giftBoxOrderId && !r.IsDeleted,
                r => r.GiftBoxOrder, r => r.GiftBoxOrder.Order);
        }

        public async Task<List<Review>> GetReviewsByOrderIdAsync(Guid orderId)
        {
            var result = await GetAllAsync(
                r => r.GiftBoxOrder.OrderId == orderId && !r.IsDeleted,
                query => query.OrderByDescending(r => r.CreatedAt),
                r => r.GiftBoxOrder, r => r.GiftBoxOrder.Order);
            return result.ToList();
        }

        public async Task<List<Review>> GetReviewsWithPaginationAsync(int pageNumber, int pageSize)
        {
            var pagedResult = await GetPagedAsync(
                pageNumber,
                pageSize,
                r => !r.IsDeleted,
                query => query.OrderByDescending(r => r.CreatedAt),
                r => r.GiftBoxOrder, r => r.GiftBoxOrder.Order);
            
            return pagedResult.ToList();
        }

        public async Task<double> GetAverageServiceRatingAsync()
        {
            var reviews = await GetAllAsync(r => !r.IsDeleted);
            return reviews.Any() ? reviews.Average(r => r.ServiceQualityRating) : 0;
        }

        public async Task<double> GetAverageProductRatingAsync()
        {
            var reviews = await GetAllAsync(r => !r.IsDeleted);
            return reviews.Any() ? reviews.Average(r => r.ProductQualityRating) : 0;
        }

        public async Task<int> GetTotalReviewsCountAsync()
        {
            return await CountAsync(r => !r.IsDeleted);
        }
    }
}
