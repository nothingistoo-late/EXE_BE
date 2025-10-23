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

        public async Task<Review?> GetByOrderIdAsync(Guid orderId)
        {
            return await FirstOrDefaultAsync(
                r => r.OrderId == orderId && !r.IsDeleted,
                r => r.Order);
        }

        public async Task<List<Review>> GetReviewsByOrderIdAsync(Guid orderId)
        {
            var result = await GetAllAsync(
                r => r.OrderId == orderId && !r.IsDeleted,
                query => query.OrderByDescending(r => r.CreatedAt),
                r => r.Order);
            return result.ToList();
        }

        public async Task<List<Review>> GetReviewsWithPaginationAsync(int pageNumber, int pageSize)
        {
            var pagedResult = await GetPagedAsync(
                pageNumber,
                pageSize,
                r => !r.IsDeleted,
                query => query.OrderByDescending(r => r.CreatedAt),
                r => r.Order);
            
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

        public async Task<double> GetAverageServiceRatingByBoxTypeAsync(Guid boxTypeId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Order)
                .ThenInclude(o => o.OrderDetails)
                .Where(r => !r.IsDeleted && r.Order.OrderDetails.Any(od => od.BoxTypeId == boxTypeId))
                .ToListAsync();
            return reviews.Any() ? reviews.Average(r => r.ServiceQualityRating) : 0;
        }

        public async Task<double> GetAverageProductRatingByBoxTypeAsync(Guid boxTypeId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Order)
                .ThenInclude(o => o.OrderDetails)
                .Where(r => !r.IsDeleted && r.Order.OrderDetails.Any(od => od.BoxTypeId == boxTypeId))
                .ToListAsync();
            return reviews.Any() ? reviews.Average(r => r.ProductQualityRating) : 0;
        }

        public async Task<int> GetReviewsCountByBoxTypeAsync(Guid boxTypeId)
        {
            return await _context.Reviews
                .Include(r => r.Order)
                .ThenInclude(o => o.OrderDetails)
                .CountAsync(r => !r.IsDeleted && r.Order.OrderDetails.Any(od => od.BoxTypeId == boxTypeId));
        }
    }
}
