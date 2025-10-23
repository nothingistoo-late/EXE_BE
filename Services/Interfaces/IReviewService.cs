using DTOs.ReviewDTOs.Request;
using DTOs.ReviewDTOs.Response;
using Repositories.Helpers;

namespace Services.Interfaces
{
    public interface IReviewService
    {
        Task<ApiResult<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request, Guid userId);
        Task<ApiResult<ReviewResponse>> UpdateReviewAsync(UpdateReviewRequest request, Guid userId);
        Task<ApiResult<ReviewResponse>> GetReviewByIdAsync(Guid id);
        Task<ApiResult<ReviewDetailResponse>> GetReviewDetailAsync(Guid id);
        Task<ApiResult<ReviewResponse>> GetReviewByOrderIdAsync(Guid orderId);
        Task<ApiResult<PagedList<ReviewResponse>>> GetReviewsWithPaginationAsync(int pageNumber, int pageSize);
        Task<ApiResult<List<ReviewResponse>>> GetReviewsByOrderIdAsync(Guid orderId);
        Task<ApiResult<object>> GetReviewStatisticsAsync();
        Task<ApiResult<object>> GetReviewStatisticsByBoxTypeAsync(Guid boxTypeId);
        Task<ApiResult<bool>> DeleteReviewAsync(Guid id, Guid userId);
    }
}

