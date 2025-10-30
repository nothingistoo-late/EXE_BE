using DTOs;
using DTOs.WeeklyBlindBoxSubscription.Request;
using DTOs.WeeklyBlindBoxSubscription.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IWeeklyBlindBoxSubscriptionService
    {
        /// <summary>
        /// Đăng ký gói BlindBox theo tuần
        /// </summary>
        Task<ApiResult<WeeklyBlindBoxSubscriptionResponse>> CreateSubscriptionAsync(CreateWeeklyBlindBoxSubscriptionRequest request);

        /// <summary>
        /// Lấy tất cả subscriptions của user hiện tại
        /// </summary>
        Task<ApiResult<List<WeeklyBlindBoxSubscriptionResponse>>> GetMySubscriptionsAsync();

        /// <summary>
        /// Lấy thông tin chi tiết subscription theo ID
        /// </summary>
        Task<ApiResult<WeeklyBlindBoxSubscriptionResponse>> GetSubscriptionByIdAsync(Guid subscriptionId);

        /// <summary>
        /// Gia hạn gói subscription
        /// </summary>
        Task<ApiResult<WeeklyBlindBoxSubscriptionResponse>> RenewSubscriptionAsync(RenewWeeklyBlindBoxSubscriptionRequest request);

        /// <summary>
        /// Admin: Hoãn giao hàng cho 1 tuần cụ thể
        /// </summary>
        Task<ApiResult<bool>> PauseWeekDeliveryAsync(PauseWeekDeliveryRequest request);

        /// <summary>
        /// Admin: Lấy tất cả subscriptions (quản lý)
        /// </summary>
        Task<ApiResult<List<WeeklyBlindBoxSubscriptionResponse>>> GetAllSubscriptionsAsync();

        /// <summary>
        /// Xem thông tin delivery schedule của subscription
        /// </summary>
        Task<ApiResult<List<WeeklyDeliveryScheduleResponse>>> GetDeliverySchedulesAsync(Guid subscriptionId);

        /// <summary>
        /// Lấy danh sách các delivery sắp đến để admin xem và quản lý
        /// </summary>
        Task<ApiResult<List<WeeklyDeliveryScheduleResponse>>> GetPendingDeliveriesAsync();

        /// <summary>
        /// Admin: Đánh dấu đã giao hàng cho 1 lần giao cụ thể
        /// </summary>
        Task<ApiResult<bool>> MarkDeliveryAsync(MarkDeliveryRequest request);

        /// <summary>
        /// Admin: Lấy danh sách các delivery đã bị hoãn và chưa được giao bù lại
        /// </summary>
        Task<ApiResult<List<WeeklyDeliveryScheduleResponse>>> GetPausedDeliveriesAsync();

        /// <summary>
        /// Admin: Resume (bỏ hoãn) và schedule lại delivery
        /// </summary>
        Task<ApiResult<bool>> ResumeDeliveryAsync(ResumeDeliveryRequest request);
    }
}

