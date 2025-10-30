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
                // Check if order exists
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(request.OrderId);
                if (order == null)
                {
                    return ApiResult<ReviewResponse>.Failure(new ArgumentException("Không tìm thấy đơn hàng"));
                }

                // Check if review already exists for this order
                var existingReview = await _unitOfWork.ReviewRepository.GetByOrderIdAsync(request.OrderId);
                if (existingReview != null)
                {
                    return ApiResult<ReviewResponse>.Failure(new InvalidOperationException("Đánh giá cho đơn hàng này đã tồn tại"));
                }

                var review = new Review
                {
                    OrderId = request.OrderId,
                    ServiceQualityRating = request.ServiceQualityRating,
                    ProductQualityRating = request.ProductQualityRating,
                    ReviewContent = request.ReviewContent,
                    IsDeleted = false
                };

                await CreateAsync(review);

                var response = new ReviewResponse
                {
                    Id = review.Id,
                    OrderId = review.OrderId,
                    ServiceQualityRating = review.ServiceQualityRating,
                    ProductQualityRating = review.ProductQualityRating,
                    ReviewContent = review.ReviewContent,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };

                return ApiResult<ReviewResponse>.Success(response, "Tạo đánh giá thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewResponse>.Failure(new Exception($"Lỗi tạo đánh giá: {ex.Message}"));
            }
        }

        public async Task<ApiResult<ReviewResponse>> UpdateReviewAsync(UpdateReviewRequest request, Guid userId)
        {
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(request.Id);
                if (review == null || review.IsDeleted)
                {
                    return ApiResult<ReviewResponse>.Failure(new ArgumentException("Không tìm thấy đánh giá"));
                }

                review.ServiceQualityRating = request.ServiceQualityRating;
                review.ProductQualityRating = request.ProductQualityRating;
                review.ReviewContent = request.ReviewContent;

                await UpdateAsync(review);

                var response = new ReviewResponse
                {
                    Id = review.Id,
                    OrderId = review.OrderId,
                    ServiceQualityRating = review.ServiceQualityRating,
                    ProductQualityRating = review.ProductQualityRating,
                    ReviewContent = review.ReviewContent,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };

                return ApiResult<ReviewResponse>.Success(response, "Cập nhật đánh giá thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewResponse>.Failure(new Exception($"Lỗi cập nhật đánh giá: {ex.Message}"));
            }
        }

        public async Task<ApiResult<ReviewResponse>> GetReviewByIdAsync(Guid id)
        {
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(id);
                if (review == null || review.IsDeleted)
                {
                    return ApiResult<ReviewResponse>.Failure(new ArgumentException("Không tìm thấy đánh giá"));
                }

                var response = new ReviewResponse
                {
                    Id = review.Id,
                    OrderId = review.OrderId,
                    ServiceQualityRating = review.ServiceQualityRating,
                    ProductQualityRating = review.ProductQualityRating,
                    ReviewContent = review.ReviewContent,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };

                return ApiResult<ReviewResponse>.Success(response, "Lấy đánh giá thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewResponse>.Failure(new Exception($"Lỗi lấy đánh giá: {ex.Message}"));
            }
        }

        public async Task<ApiResult<ReviewDetailResponse>> GetReviewDetailAsync(Guid id)
        {
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(id);
                if (review == null || review.IsDeleted)
                {
                    return ApiResult<ReviewDetailResponse>.Failure(new ArgumentException("Không tìm thấy đánh giá"));
                }

                var order = await _unitOfWork.OrderRepository.GetByIdAsync(review.OrderId);
                if (order == null)
                {
                    return ApiResult<ReviewDetailResponse>.Failure(new ArgumentException("Không tìm thấy đơn hàng"));
                }
                var customer = await _unitOfWork.CustomerRepository.FirstOrDefaultAsync(o=> o.UserId== order.UserId);

                var response = new ReviewDetailResponse
                {
                    Id = review.Id,
                    OrderId = review.OrderId,
                    ServiceQualityRating = review.ServiceQualityRating,
                    ProductQualityRating = review.ProductQualityRating,
                    ReviewContent = review.ReviewContent,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt,
                    OrderCode = order.PayOSOrderCode ?? order.Id.ToString(),
                    CustomerName = order.User?.FirstName + " " + order.User?.LastName ?? "N/A",
                    CustomerPhone = order.User?.PhoneNumber ?? "N/A",
                    CustomerAddress = customer.Address ?? "N/A" 
                };

                return ApiResult<ReviewDetailResponse>.Success(response, "Lấy chi tiết đánh giá thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewDetailResponse>.Failure(new Exception($"Lỗi lấy chi tiết đánh giá: {ex.Message}"));
            }
        }

        public async Task<ApiResult<ReviewResponse>> GetReviewByOrderIdAsync(Guid orderId)
        {
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByOrderIdAsync(orderId);
                if (review == null)
                {
                    return ApiResult<ReviewResponse>.Failure(new ArgumentException("Không tìm thấy đánh giá cho đơn hàng này"));
                }

                var response = new ReviewResponse
                {
                    Id = review.Id,
                    OrderId = review.OrderId,
                    ServiceQualityRating = review.ServiceQualityRating,
                    ProductQualityRating = review.ProductQualityRating,
                    ReviewContent = review.ReviewContent,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };

                return ApiResult<ReviewResponse>.Success(response, "Lấy đánh giá thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<ReviewResponse>.Failure(new Exception($"Lỗi lấy đánh giá: {ex.Message}"));
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
                    OrderId = r.OrderId,
                    ServiceQualityRating = r.ServiceQualityRating,
                    ProductQualityRating = r.ProductQualityRating,
                    ReviewContent = r.ReviewContent,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                }).ToList();

                var pagedList = new PagedList<ReviewResponse>(reviewResponses, totalCount, pageNumber, pageSize);

                return ApiResult<PagedList<ReviewResponse>>.Success(pagedList, "Lấy danh sách đánh giá thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<PagedList<ReviewResponse>>.Failure(new Exception($"Lỗi lấy danh sách đánh giá: {ex.Message}"));
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
                    OrderId = r.OrderId,
                    ServiceQualityRating = r.ServiceQualityRating,
                    ProductQualityRating = r.ProductQualityRating,
                    ReviewContent = r.ReviewContent,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                }).ToList();

                return ApiResult<List<ReviewResponse>>.Success(reviewResponses, "Lấy danh sách đánh giá thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<List<ReviewResponse>>.Failure(new Exception($"Lỗi lấy đánh giá theo đơn hàng: {ex.Message}"));
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

                return ApiResult<object>.Success(statistics, "Lấy thống kê thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<object>.Failure(new Exception($"Lỗi lấy thống kê đánh giá: {ex.Message}"));
            }
        }

        public async Task<ApiResult<object>> GetReviewStatisticsByBoxTypeAsync(Guid boxTypeId)
        {
            try
            {
                // Check if box type exists
                var boxType = await _unitOfWork.BoxTypeRepository.GetByIdAsync(boxTypeId);
                if (boxType == null)
                {
                    return ApiResult<object>.Failure(new ArgumentException("Không tìm thấy loại hộp"));
                }

                var averageServiceRating = await _unitOfWork.ReviewRepository.GetAverageServiceRatingByBoxTypeAsync(boxTypeId);
                var averageProductRating = await _unitOfWork.ReviewRepository.GetAverageProductRatingByBoxTypeAsync(boxTypeId);
                var totalReviews = await _unitOfWork.ReviewRepository.GetReviewsCountByBoxTypeAsync(boxTypeId);

                var statistics = new
                {
                    BoxTypeId = boxTypeId,
                    BoxTypeName = boxType.Name,
                    AverageServiceRating = Math.Round(averageServiceRating, 2),
                    AverageProductRating = Math.Round(averageProductRating, 2),
                    TotalReviews = totalReviews
                };

                return ApiResult<object>.Success(statistics, "Lấy thống kê đánh giá theo loại hộp thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<object>.Failure(new Exception($"Lỗi lấy thống kê đánh giá theo loại hộp: {ex.Message}"));
            }
        }

        public async Task<ApiResult<bool>> DeleteReviewAsync(Guid id, Guid userId)
        {
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(id);
                if (review == null || review.IsDeleted)
                {
                    return ApiResult<bool>.Failure(new ArgumentException("Không tìm thấy đánh giá"));
                }

                await DeleteAsync(id);

                return ApiResult<bool>.Success(true, "Xóa đánh giá thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure(new Exception($"Lỗi xóa đánh giá: {ex.Message}"));
            }
        }
    }
}
