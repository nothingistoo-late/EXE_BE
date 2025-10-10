using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using DTOs.NotificationDTOs.Request;
using DTOs.NotificationDTOs.Response;
using BusinessObjects.Common;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ICurrentUserService currentUserService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Send notification to a specific user
        /// </summary>
        /// <param name="request">Notification details</param>
        /// <returns>Send notification response</returns>
        [HttpPost("send-to-user")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SendToUser([FromBody] SendToUserNotificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var result = await _notificationService.SendToUserAsync(request, currentUserId.Value);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendToUser endpoint");
                return StatusCode(500, new { Message = "An error occurred while sending notification" });
            }
        }

        /// <summary>
        /// Send notification to multiple users
        /// </summary>
        /// <param name="request">Notification details</param>
        /// <returns>Send notification response</returns>
        [HttpPost("send-to-multiple")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SendToMultipleUsers([FromBody] SendToMultipleUsersNotificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var result = await _notificationService.SendToMultipleUsersAsync(request, currentUserId.Value);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendToMultipleUsers endpoint");
                return StatusCode(500, new { Message = "An error occurred while sending notification" });
            }
        }

        /// <summary>
        /// Send broadcast notification to all users
        /// </summary>
        /// <param name="request">Notification details</param>
        /// <returns>Send notification response</returns>
        [HttpPost("send-broadcast")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SendBroadcast([FromBody] SendBroadcastNotificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var result = await _notificationService.SendBroadcastAsync(request, currentUserId.Value);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendBroadcast endpoint");
                return StatusCode(500, new { Message = "An error occurred while sending notification" });
            }
        }

        /// <summary>
        /// Send broadcast notification to all users
        /// </summary>
        /// <param name="request">Notification details</param>
        /// <returns>Send notification response</returns>
        [HttpPost("broadcast")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SendBroadcastNotification([FromBody] SendBroadcastNotificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }
                var result = await _notificationService.SendBroadcastAsync(request, currentUserId.Value);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendBroadcastNotification endpoint");
                return StatusCode(500, new { Message = "An error occurred while sending broadcast notification" });
            }
        }

        /// <summary>
        /// Get notifications for current user with pagination and filtering
        /// </summary>
        /// <param name="request">Filter and pagination parameters</param>
        /// <returns>Paginated notifications list</returns>
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] GetNotificationsRequest request)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }
                var result = await _notificationService.GetNotificationsAsync(request, currentUserId.Value);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNotifications endpoint");
                return StatusCode(500, new { Message = "An error occurred while getting notifications" });
            }
        }

        /// <summary>
        /// Get notifications for admin view with pagination and filtering
        /// </summary>
        /// <param name="request">Filter and pagination parameters</param>
        /// <returns>Paginated notifications list</returns>
        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetNotificationsForAdmin([FromQuery] GetNotificationsRequest request)
        {
            try
            {
                var result = await _notificationService.GetNotificationsForAdminAsync(request);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNotificationsForAdmin endpoint");
                return StatusCode(500, new { Message = "An error occurred while getting notifications" });
            }
        }

        /// <summary>
        /// Get notification by ID
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>Notification details</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetNotificationById(Guid id)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }
                var result = await _notificationService.GetNotificationByIdAsync(id, currentUserId.Value);

                if (!result.IsSuccess)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNotificationById endpoint");
                return StatusCode(500, new { Message = "An error occurred while getting notification" });
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>Success status</returns>
        [HttpPut("{id:guid}/mark-read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }
                var result = await _notificationService.MarkAsReadAsync(id, currentUserId.Value);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkAsRead endpoint");
                return StatusCode(500, new { Message = "An error occurred while marking notification as read" });
            }
        }

        /// <summary>
        /// Mark all notifications as read for current user
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }
                var result = await _notificationService.MarkAllAsReadAsync(currentUserId.Value);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkAllAsRead endpoint");
                return StatusCode(500, new { Message = "An error occurred while marking all notifications as read" });
            }
        }

        /// <summary>
        /// Get unread notifications count for current user
        /// </summary>
        /// <returns>Unread count</returns>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }
                var result = await _notificationService.GetUnreadCountAsync(currentUserId.Value);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUnreadCount endpoint");
                return StatusCode(500, new { Message = "An error occurred while getting unread count" });
            }
        }

        /// <summary>
        /// Get recent notifications for current user
        /// </summary>
        /// <param name="count">Number of recent notifications to retrieve (default: 10)</param>
        /// <returns>Recent notifications list</returns>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentNotifications([FromQuery] int count = 10)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }
                var result = await _notificationService.GetRecentNotificationsAsync(currentUserId.Value, count);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecentNotifications endpoint");
                return StatusCode(500, new { Message = "An error occurred while getting recent notifications" });
            }
        }

        /// <summary>
        /// Delete notification
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }
                var result = await _notificationService.DeleteNotificationAsync(id, currentUserId.Value);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteNotification endpoint");
                return StatusCode(500, new { Message = "An error occurred while deleting notification" });
            }
        }

    }
}
