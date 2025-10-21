using BusinessObjects;
using Repositories.Commons;

namespace Repositories.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review, Guid>
    {
        Task<Review?> GetByGiftBoxOrderIdAsync(Guid giftBoxOrderId);
        Task<List<Review>> GetReviewsByOrderIdAsync(Guid orderId);
        Task<List<Review>> GetReviewsWithPaginationAsync(int pageNumber, int pageSize);
        Task<double> GetAverageServiceRatingAsync();
        Task<double> GetAverageProductRatingAsync();
        Task<int> GetTotalReviewsCountAsync();
    }
}
