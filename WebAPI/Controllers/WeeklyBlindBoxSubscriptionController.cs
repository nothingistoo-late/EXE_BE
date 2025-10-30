using DTOs;
using DTOs.WeeklyBlindBoxSubscription.Request;
using DTOs.WeeklyBlindBoxSubscription.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeeklyBlindBoxSubscriptionController : ControllerBase
    {
        private readonly IWeeklyBlindBoxSubscriptionService _subscriptionService;

        public WeeklyBlindBoxSubscriptionController(IWeeklyBlindBoxSubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        /// <summary>
        /// Đăng ký gói BlindBox theo tuần
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateWeeklyBlindBoxSubscriptionRequest request)
        {
            if (request == null)
                return BadRequest(new { isSuccess = false, message = "Request không hợp lệ" });

            var result = await _subscriptionService.CreateSubscriptionAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Lấy tất cả subscriptions của user hiện tại
        /// </summary>
        [HttpGet("my-subscriptions")]
        [Authorize]
        public async Task<IActionResult> GetMySubscriptions()
        {
            var result = await _subscriptionService.GetMySubscriptionsAsync();
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết subscription theo ID
        /// </summary>
        [HttpGet("{subscriptionId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetSubscriptionById(Guid subscriptionId)
        {
            var result = await _subscriptionService.GetSubscriptionByIdAsync(subscriptionId);
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Gia hạn gói subscription
        /// </summary>
        [HttpPost("renew")]
        [Authorize]
        public async Task<IActionResult> RenewSubscription([FromBody] RenewWeeklyBlindBoxSubscriptionRequest request)
        {
            if (request == null)
                return BadRequest(new { isSuccess = false, message = "Request không hợp lệ" });

            var result = await _subscriptionService.RenewSubscriptionAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Xem lịch giao hàng của subscription
        /// </summary>
        [HttpGet("{subscriptionId:guid}/delivery-schedules")]
        [Authorize]
        public async Task<IActionResult> GetDeliverySchedules(Guid subscriptionId)
        {
            var result = await _subscriptionService.GetDeliverySchedulesAsync(subscriptionId);
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Admin: Lấy tất cả subscriptions
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllSubscriptions()
        {
            var result = await _subscriptionService.GetAllSubscriptionsAsync();
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Admin: Hoãn giao hàng cho 1 tuần cụ thể
        /// </summary>
        [HttpPost("admin/pause-week")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PauseWeekDelivery([FromBody] PauseWeekDeliveryRequest request)
        {
            if (request == null)
                return BadRequest(new { isSuccess = false, message = "Request không hợp lệ" });

            var result = await _subscriptionService.PauseWeekDeliveryAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Admin: Lấy danh sách các delivery sắp đến để quản lý
        /// </summary>
        [HttpGet("admin/pending-deliveries")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingDeliveries()
        {
            var result = await _subscriptionService.GetPendingDeliveriesAsync();
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Admin: Đánh dấu đã giao hàng cho 1 lần giao cụ thể
        /// </summary>
        [HttpPost("admin/mark-delivery")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkDelivery([FromBody] MarkDeliveryRequest request)
        {
            if (request == null)
                return BadRequest(new { isSuccess = false, message = "Request không hợp lệ" });

            var result = await _subscriptionService.MarkDeliveryAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Admin: Lấy danh sách các delivery đã bị hoãn và chưa được giao bù lại
        /// </summary>
        [HttpGet("admin/paused-deliveries")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPausedDeliveries()
        {
            var result = await _subscriptionService.GetPausedDeliveriesAsync();
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Admin: Bỏ hoãn và schedule lại delivery (giao hàng bù lại)
        /// </summary>
        [HttpPost("admin/resume-delivery")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResumeDelivery([FromBody] ResumeDeliveryRequest request)
        {
            if (request == null)
                return BadRequest(new { isSuccess = false, message = "Request không hợp lệ" });

            var result = await _subscriptionService.ResumeDeliveryAsync(request);
            
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
    }
}

