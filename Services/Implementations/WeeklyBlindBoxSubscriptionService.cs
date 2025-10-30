using AutoMapper;
using BusinessObjects;
using BusinessObjects.Common;
using DTOs;
using DTOs.OrderDTOs.Request;
using DTOs.WeeklyBlindBoxSubscription.Request;
using DTOs.WeeklyBlindBoxSubscription.Response;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Extensions;
using Services.Commons;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class WeeklyBlindBoxSubscriptionService : BaseService<WeeklyBlindBoxSubscription, Guid>, IWeeklyBlindBoxSubscriptionService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentTime _currentTime;
        private readonly ILogger<WeeklyBlindBoxSubscriptionService> _logger;
        private readonly IOrderService _orderService;

        public WeeklyBlindBoxSubscriptionService(
            IMapper mapper,
            IGenericRepository<WeeklyBlindBoxSubscription, Guid> repository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            ILogger<WeeklyBlindBoxSubscriptionService> logger,
            IOrderService orderService)
            : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _currentTime = currentTime;
            _logger = logger;
            _orderService = orderService;
        }

        public async Task<ApiResult<WeeklyBlindBoxSubscriptionResponse>> CreateSubscriptionAsync(CreateWeeklyBlindBoxSubscriptionRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var userId = _currentUserService.GetUserId();
                    if (userId == null)
                        return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(new UnauthorizedAccessException("Không tìm thấy thông tin user"));

                    // 1. Validate BoxType
                    var boxType = await _unitOfWork.BoxTypeRepository.GetByIdAsync(request.BoxTypeId);
                    if (boxType == null || boxType.Name != "Blind Box")
                        return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(new ArgumentException("BoxTypeId không hợp lệ hoặc không phải Blind Box"));

                    // 2. Validate start date (phải là thứ 2)
                    var startDate = request.StartDate.Date;
                    while (startDate.DayOfWeek != DayOfWeek.Monday)
                        startDate = startDate.AddDays(1);

                    if (startDate < _currentTime.GetVietnamTime().Date)
                        return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(new ArgumentException("Ngày bắt đầu không được trong quá khứ"));

                    // 3. Tính giá (ví dụ: Blind Box 150k/box, gói tuần 2 box = 250k thay vì 300k)
                    var normalBoxPrice = boxType.Price;
                    var boxesPerWeek = 2;
                    var normalWeeklyPrice = normalBoxPrice * boxesPerWeek; // 300k
                    var weeklyPackagePrice = normalWeeklyPrice * 0.85; // Giảm 15% = ~255k (có thể config)
                    var perBoxPrice = weeklyPackagePrice / boxesPerWeek; // ~127.5k/box
                    var totalPrice = weeklyPackagePrice * request.DurationWeeks;
                    var savingsPerWeek = normalWeeklyPrice - weeklyPackagePrice;

                    // 4. Tính ngày kết thúc
                    var endDate = startDate.AddDays(request.DurationWeeks * 7 - 1); // Trừ 1 vì bắt đầu từ thứ 2

                    // 5. Tạo subscription
                    var subscription = new WeeklyBlindBoxSubscription
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId.Value,
                        BoxTypeId = request.BoxTypeId,
                        StartDate = startDate,
                        EndDate = endDate,
                        DurationWeeks = request.DurationWeeks,
                        WeeklyPrice = weeklyPackagePrice,
                        TotalPrice = totalPrice,
                        PerBoxPrice = perBoxPrice,
                        PaymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod),
                        PaymentStatus = PaymentStatus.Pending,
                        FirstDeliveryDay = request.FirstDeliveryDay,
                        SecondDeliveryDay = request.SecondDeliveryDay,
                        Status = WeeklySubscriptionStatus.Active,
                        Address = request.Address,
                        DeliveryTo = request.DeliveryTo,
                        PhoneNumber = request.PhoneNumber,
                        AllergyNote = request.AllergyNote,
                        PreferenceNote = request.PreferenceNote,
                        CreatedAt = _currentTime.GetVietnamTime(),
                        UpdatedAt = _currentTime.GetVietnamTime(),
                        CreatedBy = userId.Value,
                        UpdatedBy = userId.Value
                    };

                    await _repository.AddAsync(subscription);

                    // 6. Tạo 1 order để thanh toán cho toàn bộ gói
                    var orderRequest = new CreateOrderRequest
                    {
                        UserId = userId.Value,
                        Items = new List<CreateOrderDetailRequest>
                        {
                            new CreateOrderDetailRequest
                            {
                                BoxTypeId = request.BoxTypeId,
                                Quantity = boxesPerWeek * request.DurationWeeks // Tổng số box = 2 box/tuần × số tuần
                            }
                        },
                        Address = request.Address,
                        DeliveryTo = request.DeliveryTo,
                        PhoneNumber = request.PhoneNumber,
                        DeliveryMethod = DeliveryMethod.Standard,
                        PaymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod),
                        AllergyNote = request.AllergyNote,
                        PreferenceNote = request.PreferenceNote
                    };

                    var orderResult = await _orderService.CreateOrderAsync(orderRequest);
                    if (!orderResult.IsSuccess)
                    {
                        return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(
                            new Exception($"Không thể tạo order: {orderResult.Exception?.Message}"));
                    }

                    // Cập nhật giá order theo giá subscription (ưu đãi) thay vì giá thông thường
                    if (orderResult.Data != null)
                    {
                        var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderResult.Data.Id);
                        if (order != null)
                        {
                            // FinalPrice = giá subscription (ưu đãi)
                            order.FinalPrice = totalPrice;
                            // TotalPrice giữ nguyên (giá thông thường) để user thấy tiết kiệm
                            order.UpdatedAt = _currentTime.GetVietnamTime();
                            await _unitOfWork.SaveChangesAsync();
                        }
                    }

                    // 7. Tạo delivery schedules cho từng tuần (không link với orders)
                    for (int week = 0; week < request.DurationWeeks; week++)
                    {
                        var weekStartDate = startDate.AddDays(week * 7);
                        var weekEndDate = weekStartDate.AddDays(6);

                        // Tính ngày giao hàng trong tuần
                        var firstDeliveryDate = GetNextWeekday(weekStartDate, request.FirstDeliveryDay);
                        var secondDeliveryDate = GetNextWeekday(weekStartDate, request.SecondDeliveryDay);

                        var schedule = new WeeklyDeliverySchedule
                        {
                            Id = Guid.NewGuid(),
                            SubscriptionId = subscription.Id,
                            WeekStartDate = weekStartDate,
                            WeekEndDate = weekEndDate,
                            FirstDeliveryDate = firstDeliveryDate,
                            SecondDeliveryDate = secondDeliveryDate,
                            CreatedAt = _currentTime.GetVietnamTime(),
                            UpdatedAt = _currentTime.GetVietnamTime(),
                            CreatedBy = userId.Value,
                            UpdatedBy = userId.Value
                        };

                        await _unitOfWork.WeeklyDeliveryScheduleRepository.AddAsync(schedule);
                    }

                    await _unitOfWork.SaveChangesAsync();

                    // 7. Map response
                    var response = await MapToResponseAsync(subscription);
                    return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Success(
                        response,
                        $"Đăng ký gói BlindBox theo tuần thành công! Giá gói: {totalPrice:N0} VNĐ, Tiết kiệm: {savingsPerWeek * request.DurationWeeks:N0} VNĐ");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating weekly blindbox subscription");
                return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<List<WeeklyBlindBoxSubscriptionResponse>>> GetMySubscriptionsAsync()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                if (userId == null)
                    return ApiResult<List<WeeklyBlindBoxSubscriptionResponse>>.Failure(new UnauthorizedAccessException("Không tìm thấy thông tin user"));

                var subscriptions = await _unitOfWork.WeeklyBlindBoxSubscriptionRepository.GetByUserIdAsync(userId.Value);

                var responses = new List<WeeklyBlindBoxSubscriptionResponse>();
                foreach (var sub in subscriptions)
                {
                    responses.Add(await MapToResponseAsync(sub));
                }

                return ApiResult<List<WeeklyBlindBoxSubscriptionResponse>>.Success(responses, "Lấy danh sách subscription thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user subscriptions");
                return ApiResult<List<WeeklyBlindBoxSubscriptionResponse>>.Failure(ex);
            }
        }

        public async Task<ApiResult<WeeklyBlindBoxSubscriptionResponse>> GetSubscriptionByIdAsync(Guid subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.WeeklyBlindBoxSubscriptionRepository.GetWithDetailsAsync(subscriptionId);
                if (subscription == null)
                    return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(new ArgumentException("Không tìm thấy subscription"));

                var userId = _currentUserService.GetUserId();
                if (userId == null || subscription.UserId != userId.Value)
                    return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(new UnauthorizedAccessException("Không có quyền truy cập subscription này"));

                var response = await MapToResponseAsync(subscription);
                return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Success(response, "Lấy thông tin subscription thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription by ID");
                return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<WeeklyBlindBoxSubscriptionResponse>> RenewSubscriptionAsync(RenewWeeklyBlindBoxSubscriptionRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var subscription = await _unitOfWork.WeeklyBlindBoxSubscriptionRepository.GetWithDetailsAsync(request.SubscriptionId);
                    if (subscription == null)
                        return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(new ArgumentException("Không tìm thấy subscription"));

                    var userId = _currentUserService.GetUserId();
                    if (userId == null || subscription.UserId != userId.Value)
                        return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(new UnauthorizedAccessException("Không có quyền gia hạn subscription này"));

                    if (subscription.Status != WeeklySubscriptionStatus.Active)
                        return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(new InvalidOperationException("Chỉ có thể gia hạn gói đang hoạt động"));

                    // Tính giá cho số tuần gia hạn
                    var boxesPerWeek = 2;
                    var additionalPrice = subscription.WeeklyPrice * request.AdditionalWeeks;
                    subscription.TotalPrice += additionalPrice;
                    subscription.EndDate = subscription.EndDate.AddDays(request.AdditionalWeeks * 7);
                    subscription.DurationWeeks += request.AdditionalWeeks;
                    subscription.UpdatedAt = _currentTime.GetVietnamTime();
                    subscription.UpdatedBy = userId.Value;

                    // Tạo order để thanh toán cho phần gia hạn
                    var orderRequest = new CreateOrderRequest
                    {
                        UserId = userId.Value,
                        Items = new List<CreateOrderDetailRequest>
                        {
                            new CreateOrderDetailRequest
                            {
                                BoxTypeId = subscription.BoxTypeId,
                                Quantity = boxesPerWeek * request.AdditionalWeeks // Tổng số box gia hạn
                            }
                        },
                        Address = subscription.Address,
                        DeliveryTo = subscription.DeliveryTo,
                        PhoneNumber = subscription.PhoneNumber,
                        DeliveryMethod = DeliveryMethod.Standard,
                        PaymentMethod = subscription.PaymentMethod,
                        AllergyNote = subscription.AllergyNote,
                        PreferenceNote = subscription.PreferenceNote
                    };

                    var orderResult = await _orderService.CreateOrderAsync(orderRequest);
                    if (!orderResult.IsSuccess)
                    {
                        return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(
                            new Exception($"Không thể tạo order để thanh toán phần gia hạn: {orderResult.Exception?.Message}"));
                    }

                    // Cập nhật giá order theo giá subscription (ưu đãi) thay vì giá thông thường
                    if (orderResult.Data != null)
                    {
                        var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderResult.Data.Id);
                        if (order != null)
                        {
                            // FinalPrice = giá subscription cho phần gia hạn (ưu đãi)
                            order.FinalPrice = additionalPrice;
                            // TotalPrice giữ nguyên (giá thông thường) để user thấy tiết kiệm
                            // Hoặc có thể update TotalPrice = additionalPrice nếu muốn
                            order.UpdatedAt = _currentTime.GetVietnamTime();
                            await _unitOfWork.SaveChangesAsync();
                        }
                    }

                    // Tạo delivery schedules cho các tuần mới
                    var lastWeekEnd = subscription.DeliverySchedules.OrderByDescending(d => d.WeekEndDate).FirstOrDefault()?.WeekEndDate ?? subscription.EndDate;
                    var newStartDate = lastWeekEnd.AddDays(1);

                    for (int week = 0; week < request.AdditionalWeeks; week++)
                    {
                        var weekStartDate = newStartDate.AddDays(week * 7);
                        var weekEndDate = weekStartDate.AddDays(6);

                        var firstDeliveryDate = GetNextWeekday(weekStartDate, subscription.FirstDeliveryDay);
                        var secondDeliveryDate = GetNextWeekday(weekStartDate, subscription.SecondDeliveryDay);

                        var schedule = new WeeklyDeliverySchedule
                        {
                            Id = Guid.NewGuid(),
                            SubscriptionId = subscription.Id,
                            WeekStartDate = weekStartDate,
                            WeekEndDate = weekEndDate,
                            FirstDeliveryDate = firstDeliveryDate,
                            SecondDeliveryDate = secondDeliveryDate,
                            CreatedAt = _currentTime.GetVietnamTime(),
                            UpdatedAt = _currentTime.GetVietnamTime(),
                            CreatedBy = userId.Value,
                            UpdatedBy = userId.Value
                        };

                        await _unitOfWork.WeeklyDeliveryScheduleRepository.AddAsync(schedule);
                    }

                    await _unitOfWork.SaveChangesAsync();

                    var response = await MapToResponseAsync(subscription);
                    return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Success(
                        response,
                        $"Gia hạn gói thành công thêm {request.AdditionalWeeks} tuần! Giá: {additionalPrice:N0} VNĐ");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing subscription");
                return ApiResult<WeeklyBlindBoxSubscriptionResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<bool>> PauseWeekDeliveryAsync(PauseWeekDeliveryRequest request)
        {
            try
            {
                var schedule = await _unitOfWork.WeeklyDeliveryScheduleRepository.GetBySubscriptionAndWeekAsync(
                    request.SubscriptionId, request.WeekStartDate);

                if (schedule == null)
                    return ApiResult<bool>.Failure(new ArgumentException("Không tìm thấy delivery schedule cho tuần này"));

                schedule.IsPaused = true;
                schedule.PauseReason = request.Reason;
                schedule.UpdatedAt = _currentTime.GetVietnamTime();
                schedule.UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty;

                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true, "Đã hoãn giao hàng cho tuần này thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing week delivery");
                return ApiResult<bool>.Failure(ex);
            }
        }

        public async Task<ApiResult<List<WeeklyBlindBoxSubscriptionResponse>>> GetAllSubscriptionsAsync()
        {
            try
            {
                var subscriptions = await _repository.GetAllAsync(
                    predicate: s => !s.IsDeleted,
                    includes: s => new { s.BoxType, s.User, s.DeliverySchedules });

                var responses = new List<WeeklyBlindBoxSubscriptionResponse>();
                foreach (var sub in subscriptions)
                {
                    responses.Add(await MapToResponseAsync(sub));
                }

                return ApiResult<List<WeeklyBlindBoxSubscriptionResponse>>.Success(responses, "Lấy danh sách subscriptions thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all subscriptions");
                return ApiResult<List<WeeklyBlindBoxSubscriptionResponse>>.Failure(ex);
            }
        }

        public async Task<ApiResult<List<WeeklyDeliveryScheduleResponse>>> GetDeliverySchedulesAsync(Guid subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.WeeklyBlindBoxSubscriptionRepository.GetWithDetailsAsync(subscriptionId);
                if (subscription == null)
                    return ApiResult<List<WeeklyDeliveryScheduleResponse>>.Failure(new ArgumentException("Không tìm thấy subscription"));

                var schedules = subscription.DeliverySchedules.OrderBy(s => s.WeekStartDate).ToList();
                var responses = schedules.Select(s => new WeeklyDeliveryScheduleResponse
                {
                    Id = s.Id,
                    SubscriptionId = s.SubscriptionId,
                    WeekStartDate = s.WeekStartDate,
                    WeekEndDate = s.WeekEndDate,
                    FirstDeliveryDate = s.FirstDeliveryDate,
                    IsFirstDelivered = s.IsFirstDelivered,
                    FirstDeliveredAt = s.FirstDeliveredAt,
                    SecondDeliveryDate = s.SecondDeliveryDate,
                    IsSecondDelivered = s.IsSecondDelivered,
                    SecondDeliveredAt = s.SecondDeliveredAt,
                    IsPaused = s.IsPaused,
                    PauseReason = s.PauseReason,
                    DeliveryCount = (s.IsFirstDelivered ? 1 : 0) + (s.IsSecondDelivered ? 1 : 0)
                }).ToList();

                return ApiResult<List<WeeklyDeliveryScheduleResponse>>.Success(responses, "Lấy lịch giao hàng thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery schedules");
                return ApiResult<List<WeeklyDeliveryScheduleResponse>>.Failure(ex);
            }
        }

        /// <summary>
        /// Lấy danh sách các delivery sắp đến để admin xem và chuẩn bị giao hàng
        /// Không tự động tạo orders, chỉ để tracking và quản lý
        /// </summary>
        public async Task<ApiResult<List<WeeklyDeliveryScheduleResponse>>> GetPendingDeliveriesAsync()
        {
            try
            {
                var currentDate = _currentTime.GetVietnamTime().Date;
                var pendingDeliveries = await _unitOfWork.WeeklyDeliveryScheduleRepository.GetPendingDeliveriesAsync(currentDate);

                var responses = pendingDeliveries
                    .Where(s => !s.IsPaused && s.Subscription.Status == WeeklySubscriptionStatus.Active)
                    .Select(s => new WeeklyDeliveryScheduleResponse
                    {
                        Id = s.Id,
                        SubscriptionId = s.SubscriptionId,
                        WeekStartDate = s.WeekStartDate,
                        WeekEndDate = s.WeekEndDate,
                        FirstDeliveryDate = s.FirstDeliveryDate,
                        IsFirstDelivered = s.IsFirstDelivered,
                        FirstDeliveredAt = s.FirstDeliveredAt,
                        SecondDeliveryDate = s.SecondDeliveryDate,
                        IsSecondDelivered = s.IsSecondDelivered,
                        SecondDeliveredAt = s.SecondDeliveredAt,
                        IsPaused = s.IsPaused,
                        PauseReason = s.PauseReason,
                        DeliveryCount = (s.IsFirstDelivered ? 1 : 0) + (s.IsSecondDelivered ? 1 : 0),
                        SubscriptionInfo = s.Subscription != null ? new SubscriptionInfo
                        {
                            UserId = s.Subscription.UserId,
                            UserName = $"{s.Subscription.User?.FirstName ?? ""} {s.Subscription.User?.LastName ?? ""}".Trim(),
                            UserEmail = s.Subscription.User?.Email ?? string.Empty,
                            Address = s.Subscription.Address,
                            PhoneNumber = s.Subscription.PhoneNumber
                        } : null
                    })
                    .ToList();

                return ApiResult<List<WeeklyDeliveryScheduleResponse>>.Success(responses, $"Có {responses.Count} delivery sắp đến");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending deliveries");
                return ApiResult<List<WeeklyDeliveryScheduleResponse>>.Failure(ex);
            }
        }

        public async Task<ApiResult<List<WeeklyDeliveryScheduleResponse>>> GetPausedDeliveriesAsync()
        {
            try
            {
                var pausedSchedules = await _unitOfWork.WeeklyDeliveryScheduleRepository.GetPausedDeliveriesAsync();

                var responses = pausedSchedules
                    .Select(s => new WeeklyDeliveryScheduleResponse
                    {
                        Id = s.Id,
                        SubscriptionId = s.SubscriptionId,
                        WeekStartDate = s.WeekStartDate,
                        WeekEndDate = s.WeekEndDate,
                        FirstDeliveryDate = s.FirstDeliveryDate,
                        IsFirstDelivered = s.IsFirstDelivered,
                        FirstDeliveredAt = s.FirstDeliveredAt,
                        SecondDeliveryDate = s.SecondDeliveryDate,
                        IsSecondDelivered = s.IsSecondDelivered,
                        SecondDeliveredAt = s.SecondDeliveredAt,
                        IsPaused = s.IsPaused,
                        PauseReason = s.PauseReason,
                        DeliveryCount = (s.IsFirstDelivered ? 1 : 0) + (s.IsSecondDelivered ? 1 : 0),
                        // Thêm thông tin subscription để admin biết
                        SubscriptionInfo = s.Subscription != null ? new SubscriptionInfo
                        {
                            UserId = s.Subscription.UserId,
                            UserName = $"{s.Subscription.User?.FirstName ?? ""} {s.Subscription.User?.LastName ?? ""}".Trim(),
                            UserEmail = s.Subscription.User?.Email ?? string.Empty,
                            Address = s.Subscription.Address,
                            PhoneNumber = s.Subscription.PhoneNumber
                        } : null
                    })
                    .ToList();

                return ApiResult<List<WeeklyDeliveryScheduleResponse>>.Success(
                    responses, 
                    $"Có {responses.Count} delivery đã bị hoãn và chưa được giao bù lại");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paused deliveries");
                return ApiResult<List<WeeklyDeliveryScheduleResponse>>.Failure(ex);
            }
        }

        public async Task<ApiResult<bool>> ResumeDeliveryAsync(ResumeDeliveryRequest request)
        {
            try
            {
                var schedule = await _unitOfWork.WeeklyDeliveryScheduleRepository.GetByIdAsync(request.ScheduleId);
                if (schedule == null)
                    return ApiResult<bool>.Failure(new ArgumentException("Không tìm thấy delivery schedule"));

                if (!schedule.IsPaused)
                    return ApiResult<bool>.Failure(new InvalidOperationException("Delivery này không bị hoãn"));

                // Bỏ hoãn
                schedule.IsPaused = false;
                schedule.PauseReason = null;

                // Cập nhật ngày giao hàng mới nếu có
                if (request.NewFirstDeliveryDate.HasValue)
                {
                    schedule.FirstDeliveryDate = request.NewFirstDeliveryDate.Value;
                }

                if (request.NewSecondDeliveryDate.HasValue)
                {
                    schedule.SecondDeliveryDate = request.NewSecondDeliveryDate.Value;
                }

                schedule.UpdatedAt = _currentTime.GetVietnamTime();
                schedule.UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty;

                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true, "Đã bỏ hoãn và schedule lại delivery thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming delivery");
                return ApiResult<bool>.Failure(ex);
            }
        }

        public async Task<ApiResult<bool>> MarkDeliveryAsync(MarkDeliveryRequest request)
        {
            try
            {
                var schedule = await _unitOfWork.WeeklyDeliveryScheduleRepository.GetByIdAsync(request.ScheduleId);
                if (schedule == null)
                    return ApiResult<bool>.Failure(new ArgumentException("Không tìm thấy delivery schedule"));

                var deliveredAt = request.DeliveredAt ?? _currentTime.GetVietnamTime();

                if (request.DeliveryNumber == 1)
                {
                    if (schedule.IsFirstDelivered)
                        return ApiResult<bool>.Failure(new InvalidOperationException("Lần giao hàng 1 đã được đánh dấu giao rồi"));

                    schedule.IsFirstDelivered = true;
                    schedule.FirstDeliveredAt = deliveredAt;
                }
                else if (request.DeliveryNumber == 2)
                {
                    if (schedule.IsSecondDelivered)
                        return ApiResult<bool>.Failure(new InvalidOperationException("Lần giao hàng 2 đã được đánh dấu giao rồi"));

                    schedule.IsSecondDelivered = true;
                    schedule.SecondDeliveredAt = deliveredAt;
                }
                else
                {
                    return ApiResult<bool>.Failure(new ArgumentException("DeliveryNumber phải là 1 hoặc 2"));
                }

                schedule.UpdatedAt = _currentTime.GetVietnamTime();
                schedule.UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty;

                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true, $"Đã đánh dấu giao hàng lần {request.DeliveryNumber} thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking delivery");
                return ApiResult<bool>.Failure(ex);
            }
        }

        // Helper methods
        private DateTime GetNextWeekday(DateTime startDate, DayOfWeek targetDay)
        {
            var currentDay = startDate.DayOfWeek;
            var daysToAdd = ((int)targetDay - (int)currentDay + 7) % 7;
            return startDate.AddDays(daysToAdd);
        }


        private async Task<WeeklyBlindBoxSubscriptionResponse> MapToResponseAsync(WeeklyBlindBoxSubscription subscription)
        {
            var normalBoxPrice = subscription.BoxType?.Price ?? subscription.PerBoxPrice * 2;
            var normalWeeklyPrice = normalBoxPrice * 2;
            var savingsPerWeek = normalWeeklyPrice - subscription.WeeklyPrice;

                var deliverySchedules = subscription.DeliverySchedules.OrderBy(d => d.WeekStartDate).Select(s => new WeeklyDeliveryScheduleResponse
                {
                    Id = s.Id,
                    SubscriptionId = s.SubscriptionId,
                    WeekStartDate = s.WeekStartDate,
                    WeekEndDate = s.WeekEndDate,
                    FirstDeliveryDate = s.FirstDeliveryDate,
                    IsFirstDelivered = s.IsFirstDelivered,
                    FirstDeliveredAt = s.FirstDeliveredAt,
                    SecondDeliveryDate = s.SecondDeliveryDate,
                    IsSecondDelivered = s.IsSecondDelivered,
                    SecondDeliveredAt = s.SecondDeliveredAt,
                    IsPaused = s.IsPaused,
                    PauseReason = s.PauseReason,
                    DeliveryCount = (s.IsFirstDelivered ? 1 : 0) + (s.IsSecondDelivered ? 1 : 0)
                }).ToList();

            var user = subscription.User;
            var remainingWeeks = Math.Max(0, (subscription.EndDate.Date - _currentTime.GetVietnamTime().Date).Days / 7);

            return new WeeklyBlindBoxSubscriptionResponse
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                UserName = $"{user.FirstName} {user.LastName}",
                UserEmail = user.Email ?? string.Empty,
                BoxTypeId = subscription.BoxTypeId,
                BoxTypeName = subscription.BoxType?.Name ?? "Blind Box",
                BoxTypePrice = subscription.BoxType?.Price ?? 0,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                DurationWeeks = subscription.DurationWeeks,
                RemainingWeeks = remainingWeeks,
                WeeklyPrice = subscription.WeeklyPrice,
                TotalPrice = subscription.TotalPrice,
                PerBoxPrice = subscription.PerBoxPrice,
                SavingsPerWeek = savingsPerWeek,
                PaymentMethod = subscription.PaymentMethod.ToString(),
                PaymentStatus = subscription.PaymentStatus.ToString(),
                FirstDeliveryDay = subscription.FirstDeliveryDay.ToString(),
                SecondDeliveryDay = subscription.SecondDeliveryDay.ToString(),
                Status = subscription.Status.ToString(),
                Address = subscription.Address,
                DeliveryTo = subscription.DeliveryTo,
                PhoneNumber = subscription.PhoneNumber,
                AllergyNote = subscription.AllergyNote,
                PreferenceNote = subscription.PreferenceNote,
                DeliverySchedules = deliverySchedules
            };
        }
    }
}

