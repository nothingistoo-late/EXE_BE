using BusinessObjects;
using DTOs.ReviewDTOs.Request;
using DTOs.ReviewDTOs.Response;
using Microsoft.EntityFrameworkCore;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;
using Services.Commons;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ReviewService : BaseService<Review, Guid>, IReviewService
    {
        public ReviewService(
            IGenericRepository<Review, Guid> repository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime) : base(repository, currentUserService, unitOfWork, currentTime)
        {
        }

        public async Task<ApiResult<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request, Guid userId)
        {
            try
            {
                // Check if gift box order exists
                var giftBoxOrder = await _unitOfWork.GiftBoxOrderRepository.GetByIdAsync(request.GiftBoxOrderId);
                if (giftBoxOrder == null)
                {
                    return ApiResult<ReviewResponse>.Failure(new ArgumentException("Gift box order not found"));
                }

                // Check if review already exists for this gift box order
                var existingReview = await _unitOfWork.ReviewRepository.GetByGiftBoxOrderIdAsync(request.GiftBoxOrderId);
                if (existingReview != null)
                {
                    return ApiResult<ReviewResponse>.Failure(new InvalidOperationException("Review already exists for this gift box order"));
                }

                var review = new Review
                {
                    GiftBoxOrderId = request.GiftBoxOrderId,
                    ServiceQualityRating = request.ServiceQualityRating,
                    ProductQualityRating = request.ProductQualityRating,
                    ReviewContent = request.ReviewContent,
                    IsDeleted = false
                };

                await CreateAsync(review);

                var response = new ReviewResponse
                {
                    Id = review.Id,
                    GiftBoxOrderId = review.GiftBoxOrderId,
                    ServiceQualityRating = review.ServiceQualityRating,
                    ProductQualityRating = review.ProductQualityRating,
                    ReviewContent = review.ReviewContent,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };

                return ApiResult<ReviewResponse>.Success(response, "Review created successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewResponse>.Failure(new Exception($"Error creating review: {ex.Message}"));
            }
        }

        public async Task<ApiResult<ReviewResponse>> UpdateReviewAsync(UpdateReviewRequest request, Guid userId)
        {
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(request.Id);
                if (review == null || review.IsDeleted)
                {
                    return ApiResult<ReviewResponse>.Failure(new ArgumentException("Review not found"));
                }

                review.ServiceQualityRating = request.ServiceQualityRating;
                review.ProductQualityRating = request.ProductQualityRating;
                review.ReviewContent = request.ReviewContent;

                await UpdateAsync(review);

                var response = new ReviewResponse
                {
                    Id = review.Id,
                    GiftBoxOrderId = review.GiftBoxOrderId,
                    ServiceQualityRating = review.ServiceQualityRating,
                    ProductQualityRating = review.ProductQualityRating,
                    ReviewContent = review.ReviewContent,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };

                return ApiResult<ReviewResponse>.Success(response, "Review updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewResponse>.Failure(new Exception($"Error updating review: {ex.Message}"));
            }
        }

        public async Task<ApiResult<ReviewResponse>> GetReviewByIdAsync(Guid id)
        {
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(id);
                if (review == null || review.IsDeleted)
                {
                    return ApiResult<ReviewResponse>.Failure(new ArgumentException("Review not found"));
                }

                var response = new ReviewResponse
                {
                    Id = review.Id,
                    GiftBoxOrderId = review.GiftBoxOrderId,
                    ServiceQualityRating = review.ServiceQualityRating,
                    ProductQualityRating = review.ProductQualityRating,
                    ReviewContent = review.ReviewContent,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };

                return ApiResult<ReviewResponse>.Success(response, "Review retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewResponse>.Failure(new Exception($"Error getting review: {ex.Message}"));
            }
        }

        public async Task<ApiResult<ReviewDetailResponse>> GetReviewDetailAsync(Guid id)
        {
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(id);
                if (review == null || review.IsDeleted)
                {
                    return ApiResult<ReviewDetailResponse>.Failure(new ArgumentException("Review not found"));
                }

                var giftBoxOrder = await _unitOfWork.GiftBoxOrderRepository.GetByIdAsync(review.GiftBoxOrderId);
                if (giftBoxOrder == null)
                {
                    return ApiResult<ReviewDetailResponse>.Failure(new ArgumentException("Gift box order not found"));
                }

                var response = new ReviewDetailResponse
                {
                    Id = review.Id,
                    GiftBoxOrderId = review.GiftBoxOrderId,
                    ServiceQualityRating = review.ServiceQualityRating,
                    ProductQualityRating = review.ProductQualityRating,
                    ReviewContent = review.ReviewContent,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt,
                    OrderId = giftBoxOrder.OrderId.ToString(),
                    GreetingMessage = giftBoxOrder.GreetingMessage,
                    BoxDescription = giftBoxOrder.BoxDescription,
                    LetterScription = giftBoxOrder.LetterScription
                };

                return ApiResult<ReviewDetailResponse>.Success(response, "Review detail retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewDetailResponse>.Failure(new Exception($"Error getting review detail: {ex.Message}"));
            }
        }

        public async Task<ApiResult<ReviewResponse>> GetReviewByGiftBoxOrderIdAsync(Guid giftBoxOrderId)
        {
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByGiftBoxOrderIdAsync(giftBoxOrderId);
                if (review == null)
                {
                    return ApiResult<ReviewResponse>.Failure(new ArgumentException("Review not found for this gift box order"));
                }

                var response = new ReviewResponse
                {
                    Id = review.Id,
                    GiftBoxOrderId = review.GiftBoxOrderId,
                    ServiceQualityRating = review.ServiceQualityRating,
                    ProductQualityRating = review.ProductQualityRating,
                    ReviewContent = review.ReviewContent,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };

                return ApiResult<ReviewResponse>.Success(response, "Review retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewResponse>.Failure(new Exception($"Error getting review: {ex.Message}"));
            }
        }

        public async Task<ApiResult<PagedList<ReviewResponse>>> GetReviewsWithPaginationAsync(int pageNumber, int pageSize)
        {
            try
            {
                var reviews = await _unitOfWork.ReviewRepository.GetReviewsWithPaginationAsync(pageNumber, pageSize);
                var totalCount = await _unitOfWork.ReviewRepository.GetTotalReviewsCountAsync();

                var reviewResponses = reviews.Select(r => new ReviewResponse
                {
                    Id = r.Id,
                    GiftBoxOrderId = r.GiftBoxOrderId,
                    ServiceQualityRating = r.ServiceQualityRating,
                    ProductQualityRating = r.ProductQualityRating,
                    ReviewContent = r.ReviewContent,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                }).ToList();

                var pagedList = new PagedList<ReviewResponse>(reviewResponses, totalCount, pageNumber, pageSize);

                return ApiResult<PagedList<ReviewResponse>>.Success(pagedList, "Reviews retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<PagedList<ReviewResponse>>.Failure(new Exception($"Error getting reviews: {ex.Message}"));
            }
        }

        public async Task<ApiResult<List<ReviewResponse>>> GetReviewsByOrderIdAsync(Guid orderId)
        {
            try
            {
                var reviews = await _unitOfWork.ReviewRepository.GetReviewsByOrderIdAsync(orderId);

                var reviewResponses = reviews.Select(r => new ReviewResponse
                {
                    Id = r.Id,
                    GiftBoxOrderId = r.GiftBoxOrderId,
                    ServiceQualityRating = r.ServiceQualityRating,
                    ProductQualityRating = r.ProductQualityRating,
                    ReviewContent = r.ReviewContent,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                }).ToList();

                return ApiResult<List<ReviewResponse>>.Success(reviewResponses, "Reviews retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<List<ReviewResponse>>.Failure(new Exception($"Error getting reviews by order: {ex.Message}"));
            }
        }

        public async Task<ApiResult<object>> GetReviewStatisticsAsync()
        {
            try
            {
                var averageServiceRating = await _unitOfWork.ReviewRepository.GetAverageServiceRatingAsync();
                var averageProductRating = await _unitOfWork.ReviewRepository.GetAverageProductRatingAsync();
                var totalReviews = await _unitOfWork.ReviewRepository.GetTotalReviewsCountAsync();

                var statistics = new
                {
                    AverageServiceRating = Math.Round(averageServiceRating, 2),
                    AverageProductRating = Math.Round(averageProductRating, 2),
                    TotalReviews = totalReviews
                };

                return ApiResult<object>.Success(statistics, "Statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<object>.Failure(new Exception($"Error getting review statistics: {ex.Message}"));
            }
        }

        public async Task<ApiResult<bool>> DeleteReviewAsync(Guid id, Guid userId)
        {
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(id);
                if (review == null || review.IsDeleted)
                {
                    return ApiResult<bool>.Failure(new ArgumentException("Review not found"));
                }

                await DeleteAsync(id);

                return ApiResult<bool>.Success(true, "Review deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure(new Exception($"Error deleting review: {ex.Message}"));
            }
        }
    }
}
