using BusinessObjects;
using Repositories.Commons;

namespace Repositories.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review, Guid>
    {
        Task<Review?> GetByOrderIdAsync(Guid orderId);
        Task<List<Review>> GetReviewsByOrderIdAsync(Guid orderId);
        Task<List<Review>> GetReviewsWithPaginationAsync(int pageNumber, int pageSize);
        Task<double> GetAverageServiceRatingAsync();
        Task<double> GetAverageProductRatingAsync();
        Task<int> GetTotalReviewsCountAsync();
        Task<double> GetAverageServiceRatingByBoxTypeAsync(Guid boxTypeId);
        Task<double> GetAverageProductRatingByBoxTypeAsync(Guid boxTypeId);
        Task<int> GetReviewsCountByBoxTypeAsync(Guid boxTypeId);
    }
}
