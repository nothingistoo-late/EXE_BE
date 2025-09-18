using AutoMapper;
using DTOs.CustomerSubscriptionRequest.Request;
using DTOs.CustomerSubscriptionRequest.Respond;
using Services.Commons;
using Services.Helpers;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class CustomerSubscriptionService : BaseService<CustomerSubscription, Guid>, ICustomerSubscriptionService
    {

        private readonly IMapper _mapper;

        public CustomerSubscriptionService(IMapper mapper, IGenericRepository<CustomerSubscription, Guid> repository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ICurrentTime currentTime) : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _mapper = mapper;
        }

        public async Task<ApiResult<List<CustomerSubscriptionResponse>>> GetAllSubscriptionsAsync()
        {
            try
            {
                var subs = await _unitOfWork.CustomerSubscriptionRepository
                    .GetAllAsync(orderBy:s=> s.OrderByDescending(x=> x.StartDate) ,includes: s => s.HealthSurvey); // nhớ Include health survey

                var data = _mapper.Map<List<CustomerSubscriptionResponse>>(subs);
                return ApiResult<List<CustomerSubscriptionResponse>>.Success(data,"Lấy tất cả gói đăng kí thành công!!");
            }
            catch (Exception ex)
            {
                return ApiResult<List<CustomerSubscriptionResponse>>
                    .Failure(new Exception("Có lỗi xảy ra khi lấy danh sách subscription: " + ex.Message));
            }
        }

        public async Task<ApiResult<CustomerSubscriptionResponse>> GetCustomerSubscriptionsAsync(Guid customerId)
        {
            try
            {
                if (customerId == Guid.Empty)
                    return ApiResult<CustomerSubscriptionResponse>
                        .Failure(new Exception("CustomerId không được để trống."));

                var customer = await _unitOfWork.CustomerRepository
                    .FirstOrDefaultAsync(c => c.UserId == customerId);
                if (customer == null)
                    return ApiResult<CustomerSubscriptionResponse>
                        .Failure(new Exception($"Không tìm thấy khách hàng với Id: {customerId}"));

                var subs = await _unitOfWork.CustomerSubscriptionRepository
                    .FirstOrDefaultAsync(x => x.CustomerId == customerId,
                                includes: s => s.HealthSurvey);
                if (subs == null)
                    return ApiResult<CustomerSubscriptionResponse>
                        .Failure(new Exception("Khách hàng chưa có gói dịch vụ nào!"));

                var data = _mapper.Map<CustomerSubscriptionResponse>(subs);
                return ApiResult<CustomerSubscriptionResponse>.Success(data, "Lấy gói đăng kí theo ID của khách hàng : "+customerId+" thành công!!");
            }
            catch (Exception ex)
            {
                return ApiResult<CustomerSubscriptionResponse>
                    .Failure(new Exception("Có lỗi xảy ra khi lấy subscription của khách hàng: " + ex.Message));
            }
        }

        public async Task<ApiResult<CustomerSubscriptionResponse>> GetSubscriptionByIdAsync(Guid subscriptionId)
        {
            try
            {
                if (subscriptionId == Guid.Empty)
                    return ApiResult<CustomerSubscriptionResponse>
                        .Failure(new Exception("SubscriptionId không được để trống."));

                var sub = await _unitOfWork.CustomerSubscriptionRepository
                    .GetByIdAsync(subscriptionId, s => s.HealthSurvey);

                if (sub == null)
                    return ApiResult<CustomerSubscriptionResponse>
                        .Failure(new Exception($"Không tìm thấy subscription với Id: {subscriptionId}"));

                var data = _mapper.Map<CustomerSubscriptionResponse>(sub);
                return ApiResult<CustomerSubscriptionResponse>.Success(data, "Lấy gói đăng kí thành công!!");
            }
            catch (Exception ex)
            {
                return ApiResult<CustomerSubscriptionResponse>
                    .Failure(new Exception("Có lỗi xảy ra khi lấy thông tin subscription: " + ex.Message));
            }
        }

        public async Task<ApiResult<List<MarkPaidSubscriptionResult>>> MarkPaidSubscriptionsAsync(List<Guid> subscriptionIds)
        {
            try
            {
                if (subscriptionIds == null || !subscriptionIds.Any())
                    return ApiResult<List<MarkPaidSubscriptionResult>>.Failure(
                        new Exception("Danh sách subscriptionIds không được để trống."));

                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    // 1️⃣ Lấy tất cả subscriptions theo Id
                    var subscriptions = await _unitOfWork.CustomerSubscriptionRepository
                        .GetByIdsAsync(subscriptionIds);

                    var results = new List<MarkPaidSubscriptionResult>();

                    foreach (var id in subscriptionIds)
                    {
                        var sub = subscriptions.FirstOrDefault(x => x.Id == id);
                        if (sub == null)
                        {
                            results.Add(new MarkPaidSubscriptionResult
                            {
                                SubscriptionId = id,
                                IsSuccess = false,
                                Message = "Không tìm thấy subscription."
                            });
                            continue;
                        }
                        if (sub.PaymentStatus == PaymentStatus.Paid)
                        {
                            results.Add(new MarkPaidSubscriptionResult
                            {
                                SubscriptionId = id,
                                IsSuccess = false,
                                Message = "Đơn hàng này đã thanh toán rồi."
                            });
                            continue;
                        }

                        // Cập nhật trạng thái
                        sub.PaymentStatus = PaymentStatus.Paid;
                        sub.UpdatedAt = _currentTime.GetVietnamTime();

                        results.Add(new MarkPaidSubscriptionResult
                        {
                            SubscriptionId = id,
                            IsSuccess = true,
                            Message = "Đã thanh toán thành công."
                        });
                    }

                    // 2️⃣ Lưu tất cả subscriptions thành công
                    var toUpdate = subscriptions.Where(x => x.PaymentStatus == PaymentStatus.Paid).ToList();
                    if (toUpdate.Any())
                    {
                        await _unitOfWork.CustomerSubscriptionRepository.UpdateRangeAsync(toUpdate);
                        await _unitOfWork.SaveChangesAsync(); // ✅ Bắt buộc gọi để EF lưu thay đổi

                    }

                    return ApiResult<List<MarkPaidSubscriptionResult>>.Success(results, "Hoàn tất đánh dấu thanh toán.");
                });
            }
            catch (Exception ex)
            {
                return ApiResult<List<MarkPaidSubscriptionResult>>.Failure(
                    new Exception("Có lỗi xảy ra khi đánh dấu thanh toán: " + ex.Message));
            }
        }

        public async Task<ApiResult<List<MarkPaidSubscriptionResult>>> MarkUnpaidSubscriptionsAsync(List<Guid> subscriptionIds)
        {
            try
            {
                if (subscriptionIds == null || !subscriptionIds.Any())
                    return ApiResult<List<MarkPaidSubscriptionResult>>.Failure(
                        new Exception("Danh sách subscriptionIds không được để trống."));

                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    // 1️⃣ Lấy tất cả subscriptions theo Id
                    var subscriptions = await _unitOfWork.CustomerSubscriptionRepository
                        .GetByIdsAsync(subscriptionIds);

                    var results = new List<MarkPaidSubscriptionResult>();

                    foreach (var id in subscriptionIds)
                    {
                        var sub = subscriptions.FirstOrDefault(x => x.Id == id);
                        if (sub == null)
                        {
                            results.Add(new MarkPaidSubscriptionResult
                            {
                                SubscriptionId = id,
                                IsSuccess = false,
                                Message = "Không tìm thấy subscription."
                            });
                            continue;
                        }

                        if (sub.PaymentStatus == PaymentStatus.Pending)
                        {
                            results.Add(new MarkPaidSubscriptionResult
                            {
                                SubscriptionId = id,
                                IsSuccess = false,
                                Message = "Đơn hàng này chưa thanh toán."
                            });
                            continue;
                        }

                        // Cập nhật trạng thái thành Unpaid
                        sub.PaymentStatus = PaymentStatus.Pending;
                        sub.UpdatedAt = _currentTime.GetVietnamTime();

                        results.Add(new MarkPaidSubscriptionResult
                        {
                            SubscriptionId = id,
                            IsSuccess = true,
                            Message = "Đã đánh dấu chưa thanh toán."
                        });
                    }

                    // 2️⃣ Lưu tất cả subscriptions thành công
                    var toUpdate = subscriptions.Where(x => x.PaymentStatus == PaymentStatus.Pending).ToList();
                    if (toUpdate.Any())
                    {
                        await _unitOfWork.CustomerSubscriptionRepository.UpdateRangeAsync(toUpdate);
                        await _unitOfWork.SaveChangesAsync(); // ✅ Bắt buộc gọi để EF lưu thay đổi

                    }

                    return ApiResult<List<MarkPaidSubscriptionResult>>.Success(results, "Hoàn tất đánh dấu chưa thanh toán.");
                });
            }
            catch (Exception ex)
            {
                return ApiResult<List<MarkPaidSubscriptionResult>>.Failure(
                    new Exception("Có lỗi xảy ra khi đánh dấu chưa thanh toán: " + ex.Message));
            }
        }

        public async Task<ApiResult<CustomerSubscriptionResponse>> PurchaseSubscriptionAsync(
     CustomerPurchaseSubscriptionRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    // ===== Validate input =====
                    if (request.CustomerId == Guid.Empty || request.SubscriptionPackageId == Guid.Empty)
                        return ApiResult<CustomerSubscriptionResponse>.Failure(
                            new Exception("CustomerId hoặc SubscriptionPackageId không được để trống."));

                    // 1️⃣ Check customer tồn tại
                    var customer = await _unitOfWork.CustomerRepository
                        .FirstOrDefaultAsync(c => c.UserId == request.CustomerId);
                    if (customer == null)
                        return ApiResult<CustomerSubscriptionResponse>.Failure(
                            new Exception($"Không tìm thấy khách hàng với Id: {request.CustomerId}"));

                    // 2️⃣ Check package tồn tại
                    var package = await _unitOfWork.SubscriptionPackageRepository
                        .GetByIdAsync(request.SubscriptionPackageId);
                    if (package == null)
                        return ApiResult<CustomerSubscriptionResponse>.Failure(
                            new Exception($"Không tìm thấy gói Subscription với Id: {request.SubscriptionPackageId}"));

                    // 3️⃣ Check nếu KH đã có gói active
                    var existing = await _unitOfWork.CustomerSubscriptionRepository
                        .FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId
                                                  && x.Status == CustomerSubscriptionStatus.Active);
                    if (existing != null)
                        return ApiResult<CustomerSubscriptionResponse>.Failure(
                            new Exception("Khách hàng đã có gói dịch vụ đang active!"));

                    // 4️⃣ Tính thời gian hiệu lực
                    var timeNow = _currentTime.GetVietnamTime();
                    var startDate = timeNow;
                    var endDate = package.Frequency switch
                    {
                        SubscriptionPackageFrequency.Monthly => startDate.AddMonths(1),
                        SubscriptionPackageFrequency.Weekly => startDate.AddDays(7),
                        _ => startDate.AddMonths(1)
                    };

                    // 5️⃣ Tạo subscription
                    var sub = new CustomerSubscription
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = request.CustomerId,
                        SubscriptionPackageId = request.SubscriptionPackageId,
                        StartDate = startDate,
                        EndDate = endDate,
                        Status = CustomerSubscriptionStatus.Active,
                        PaymentStatus = request.PaymentStatus,
                        PaymentMethod = request.PaymentMethod,
                        CreatedAt = timeNow
                    };

                    await _unitOfWork.CustomerSubscriptionRepository.AddAsync(sub);

                    // 6️⃣ Tạo phiếu sức khoẻ (nếu có)
                    HealthSurvey? survey = null;
                    if (!string.IsNullOrWhiteSpace(request.Allergy) ||
                        !string.IsNullOrWhiteSpace(request.Feeling))
                    {
                        survey = new HealthSurvey
                        {
                            Id = Guid.NewGuid(),
                            CustomerSubscriptionId = sub.Id,
                            Allergy = request.Allergy ?? string.Empty,
                            Feeling = request.Feeling ?? string.Empty,
                            CreatedAt = timeNow
                        };
                        await _unitOfWork.HealthSurveyRepository.AddAsync(survey);
                    }

                    await _unitOfWork.SaveChangesAsync();

                    // 7️⃣ Map sang DTO
                    var res = _mapper.Map<CustomerSubscriptionResponse>(sub);
                    if (survey != null)
                    {
                        res.HealthSurveyId = survey.Id;
                        res.Allergy = survey.Allergy;
                        res.Feeling = survey.Feeling;
                    }

                    return ApiResult<CustomerSubscriptionResponse>.Success(res, "Đăng ký gói thành công!");
                });
            }
            catch (Exception ex)
            {
                return ApiResult<CustomerSubscriptionResponse>.Failure(
                    new Exception("Có lỗi xảy ra khi mua gói dịch vụ!! " + ex.Message));
            }
        }

        public async Task<ApiResult<CustomerSubscriptionResponse>> UpdateStatusSubscriptionAsync(Guid subscriptionId)
        {
            try
            {
                if (subscriptionId == Guid.Empty)
                    return ApiResult<CustomerSubscriptionResponse>
                        .Failure(new Exception("SubscriptionId không được để trống."));

                var sub = await _unitOfWork.CustomerSubscriptionRepository
                    .FirstOrDefaultAsync(x => x.Id == subscriptionId, includes: s => s.HealthSurvey);

                if (sub == null)
                    return ApiResult<CustomerSubscriptionResponse>
                        .Failure(new Exception($"Không tìm thấy subscription với Id: {subscriptionId}"));

                // 👉 Toggle status (Active <-> Inactive)
                sub.Status = sub.Status == CustomerSubscriptionStatus.Active
                    ? CustomerSubscriptionStatus.Inactive
                    : CustomerSubscriptionStatus.Active;

                sub.UpdatedAt = _currentTime.GetVietnamTime();

                await _unitOfWork.CustomerSubscriptionRepository.UpdateAsync(sub);
                await _unitOfWork.SaveChangesAsync();

                var res = _mapper.Map<CustomerSubscriptionResponse>(sub);
                return ApiResult<CustomerSubscriptionResponse>.Success(res, "Cập nhật trạng thái thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<CustomerSubscriptionResponse>
                    .Failure(new Exception("Có lỗi xảy ra khi cập nhật trạng thái subscription: " + ex.Message));
            }
        }


    }
}
