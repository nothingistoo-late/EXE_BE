using BusinessObjects;
using BusinessObjects.Common;
using DTOs.NotificationDTOs.Request;
using DTOs.NotificationDTOs.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Helpers.Mapers;
using Services.Interfaces;
using AutoMapper;

namespace Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationService> _logger;
        private readonly ICurrentTime _currentTime;
        private readonly IMapper _mapper;

        public NotificationService(
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            UserManager<User> userManager,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<NotificationService> logger,
            ICurrentTime currentTime,
            IMapper mapper)
        {
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentTime = currentTime ?? throw new ArgumentNullException(nameof(currentTime));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ApiResult<SendToUserNotificationResponse>> SendToUserAsync(SendToUserNotificationRequest request, Guid senderId)
        {
            try
            {
                // Validate sender exists
                var sender = await _userManager.FindByIdAsync(senderId.ToString());
                if (sender == null)
                {
                    return ApiResult<SendToUserNotificationResponse>.Failure(new Exception("Sender not found"));
                }

                return await SendNotificationToUserAsync(request, senderId, request.ReceiverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user");
                return ApiResult<SendToUserNotificationResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<SendToMultipleUsersNotificationResponse>> SendToMultipleUsersAsync(SendToMultipleUsersNotificationRequest request, Guid senderId)
        {
            try
            {
                // Validate sender exists
                var sender = await _userManager.FindByIdAsync(senderId.ToString());
                if (sender == null)
                {
                    return ApiResult<SendToMultipleUsersNotificationResponse>.Failure(new Exception("Sender not found"));
                }

                return await SendNotificationToMultipleUsersAsync(request, senderId, request.ReceiverIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to multiple users");
                return ApiResult<SendToMultipleUsersNotificationResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<SendBroadcastNotificationResponse>> SendBroadcastAsync(SendBroadcastNotificationRequest request, Guid senderId)
        {
            try
            {
                // Validate sender exists
                var sender = await _userManager.FindByIdAsync(senderId.ToString());
                if (sender == null)
                {
                    return ApiResult<SendBroadcastNotificationResponse>.Failure(new Exception("Sender not found"));
                }

                return await SendBroadcastNotificationAsync(request, senderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending broadcast notification");
                return ApiResult<SendBroadcastNotificationResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<SendToUserNotificationResponse>> SendNotificationToUserAsync(BaseNotificationRequest request, Guid senderId, Guid receiverId)
        {
            try
            {
                // Validate receiver exists
                var receiver = await _userManager.FindByIdAsync(receiverId.ToString());
                if (receiver == null)
                {
                    return ApiResult<SendToUserNotificationResponse>.Failure(new Exception("Receiver not found"));
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title,
                    Content = request.Content,
                    Type = request.Type,
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    CreatedAt = _currentTime.GetCurrentTime(),
                    UpdatedAt = _currentTime.GetCurrentTime(),
                    CreatedBy = senderId,
                    UpdatedBy = senderId
                };

                await _notificationRepository.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Implement realtime notification sending (SignalR, WebSocket, etc.)

                var response = _mapper.Map<SendToUserNotificationResponse>(notification);
                response.ReceiverId = notification.ReceiverId.Value;

                return ApiResult<SendToUserNotificationResponse>.Success(response, "Notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {ReceiverId}", receiverId);
                return ApiResult<SendToUserNotificationResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<SendToMultipleUsersNotificationResponse>> SendNotificationToMultipleUsersAsync(BaseNotificationRequest request, Guid senderId, List<Guid> receiverIds)
        {
            try
            {
                var notifications = new List<Notification>();
                var validReceiverIds = new List<Guid>();
                var receiverNames = new List<string>();

                // Validate all receivers exist
                foreach (var receiverId in receiverIds)
                {
                    var receiver = await _userManager.FindByIdAsync(receiverId.ToString());
                    if (receiver != null)
                    {
                        validReceiverIds.Add(receiverId);
                        receiverNames.Add((receiver.FirstName ?? "") + " " + (receiver.LastName ?? ""));
                        notifications.Add(new Notification
                        {
                            Id = Guid.NewGuid(),
                            Title = request.Title,
                            Content = request.Content,
                            Type = request.Type,
                            SenderId = senderId,
                            ReceiverId = receiverId,
                            CreatedAt = _currentTime.GetCurrentTime(),
                            UpdatedAt = _currentTime.GetCurrentTime(),
                            CreatedBy = senderId,
                            UpdatedBy = senderId
                        });
                    }
                }

                if (!notifications.Any())
                {
                    return ApiResult<SendToMultipleUsersNotificationResponse>.Failure(new Exception("No valid receivers found"));
                }

                await _notificationRepository.AddRangeAsync(notifications);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Implement realtime notification sending (SignalR, WebSocket, etc.)

                var response = _mapper.Map<SendToMultipleUsersNotificationResponse>(notifications.First());
                response.ReceiverIds = validReceiverIds;
                response.ReceiverNames = receiverNames;
                response.TotalSent = notifications.Count;

                return ApiResult<SendToMultipleUsersNotificationResponse>.Success(response, $"Notification sent to {notifications.Count} users successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to multiple users");
                return ApiResult<SendToMultipleUsersNotificationResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<SendBroadcastNotificationResponse>> SendBroadcastNotificationAsync(BaseNotificationRequest request, Guid senderId)
        {
            try
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title,
                    Content = request.Content,
                    Type = request.Type,
                    SenderId = senderId,
                    ReceiverId = null, // null means broadcast to all users
                    CreatedAt = _currentTime.GetCurrentTime(),
                    UpdatedAt = _currentTime.GetCurrentTime(),
                    CreatedBy = senderId,
                    UpdatedBy = senderId
                };

                await _notificationRepository.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Implement realtime notification sending (SignalR, WebSocket, etc.)

                // Get total user count for broadcast
                var totalUsers = await _userRepository.GetAllAsync();
                var totalUserCount = totalUsers.Count(u => !u.IsDeleted);

                var response = _mapper.Map<SendBroadcastNotificationResponse>(notification);
                response.TotalUsers = totalUserCount;

                return ApiResult<SendBroadcastNotificationResponse>.Success(response, "Broadcast notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending broadcast notification");
                return ApiResult<SendBroadcastNotificationResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<GetNotificationsResponse>> GetNotificationsAsync(GetNotificationsRequest request, Guid userId)
        {
            try
            {
                var notifications = await _notificationRepository.GetNotificationsAsync(request, userId);
                var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);

                var response = new GetNotificationsResponse(notifications, unreadCount);

                return ApiResult<GetNotificationsResponse>.Success(response, "Notifications retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                return ApiResult<GetNotificationsResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<GetAdminNotificationsResponse>> GetNotificationsForAdminAsync(GetNotificationsRequest request)
        {
            try
            {
                var notifications = await _notificationRepository.GetNotificationsForAdminAsync(request);

                var response = new GetAdminNotificationsResponse(
                    notifications,
                    notifications.Count,
                    0, // Admin view doesn't need unread count
                    notifications.Count(n => n.IsBroadcast));

                return ApiResult<GetAdminNotificationsResponse>.Success(response, "Notifications retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for admin");
                return ApiResult<GetAdminNotificationsResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<UserNotificationResponse>> GetNotificationByIdAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var notification = await _notificationRepository.GetNotificationByIdAsync(notificationId);
                if (notification == null)
                {
                    return ApiResult<UserNotificationResponse>.Failure(new Exception("Notification not found"));
                }

                // Check if user has access to this notification
                if (notification.ReceiverId != userId && notification.ReceiverId != null)
                {
                    return ApiResult<UserNotificationResponse>.Failure(new Exception("Access denied"));
                }

                var response = _mapper.Map<UserNotificationResponse>(notification);

                return ApiResult<UserNotificationResponse>.Success(response, "Notification retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification {NotificationId} for user {UserId}", notificationId, userId);
                return ApiResult<UserNotificationResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<bool>> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var result = await _notificationRepository.MarkAsReadAsync(notificationId, userId);
                if (!result)
                {
                    return ApiResult<bool>.Failure(new Exception("Notification not found or access denied"));
                }

                return ApiResult<bool>.Success(true, "Notification marked as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", notificationId, userId);
                return ApiResult<bool>.Failure(ex);
            }
        }

        public async Task<ApiResult<bool>> MarkAllAsReadAsync(Guid userId)
        {
            try
            {
                // This would require a new method in repository to mark all notifications as read
                // For now, we'll implement a simple approach
                var notifications = await _notificationRepository.GetNotificationsForUserAsync(userId, int.MaxValue);
                var unreadNotifications = notifications.Where(n => !n.IsRead).ToList();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = _currentTime.GetCurrentTime();
                    notification.UpdatedAt = _currentTime.GetCurrentTime();
                }

                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true, $"Marked {unreadNotifications.Count} notifications as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return ApiResult<bool>.Failure(ex);
            }
        }

        public async Task<ApiResult<int>> GetUnreadCountAsync(Guid userId)
        {
            try
            {
                var count = await _notificationRepository.GetUnreadCountAsync(userId);
                return ApiResult<int>.Success(count, "Unread count retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return ApiResult<int>.Failure(ex);
            }
        }

        public async Task<ApiResult<List<UserNotificationResponse>>> GetRecentNotificationsAsync(Guid userId, int count = 10)
        {
            try
            {
                var notifications = await _notificationRepository.GetNotificationsForUserAsync(userId, count);
                
                var response = _mapper.Map<List<UserNotificationResponse>>(notifications);

                return ApiResult<List<UserNotificationResponse>>.Success(response, "Recent notifications retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent notifications for user {UserId}", userId);
                return ApiResult<List<UserNotificationResponse>>.Failure(ex);
            }
        }

        public async Task<ApiResult<bool>> DeleteNotificationAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var notification = await _notificationRepository.GetNotificationByIdAsync(notificationId);
                if (notification == null)
                {
                    return ApiResult<bool>.Failure(new Exception("Notification not found"));
                }

                // Check if user has access to this notification
                if (notification.ReceiverId != userId && notification.ReceiverId != null)
                {
                    return ApiResult<bool>.Failure(new Exception("Access denied"));
                }

                notification.IsDeleted = true;
                notification.DeletedAt = _currentTime.GetCurrentTime();
                notification.UpdatedAt = _currentTime.GetCurrentTime();

                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true, "Notification deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", notificationId, userId);
                return ApiResult<bool>.Failure(ex);
            }
        }

    }
}
