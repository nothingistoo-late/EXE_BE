using BusinessObjects;
using BusinessObjects.Common;
using DTOs.NotificationDTOs.Request;
using DTOs.NotificationDTOs.Response;
using Microsoft.EntityFrameworkCore;
using Repositories.WorkSeeds.Implements;
using AutoMapper;
using System.Linq.Expressions;

namespace Repositories.Implementations
{
    public class NotificationRepository : GenericRepository<Notification, Guid>, INotificationRepository
    {
        private readonly EXE_BE _context;
        private readonly IMapper _mapper;

        public NotificationRepository(EXE_BE context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<UserNotificationResponse>> GetNotificationsAsync(GetNotificationsRequest request, Guid userId)
        {
            // Build predicate for filtering
            Expression<Func<Notification, bool>> predicate = n => 
                !n.IsDeleted && (n.ReceiverId == userId || n.ReceiverId == null);

            // Add additional filters
            if (request.IsRead.HasValue)
            {
                predicate = CombinePredicates(predicate, n => n.IsRead == request.IsRead.Value);
            }

            if (request.Type.HasValue)
            {
                predicate = CombinePredicates(predicate, n => n.Type == request.Type.Value);
            }

            // Get notifications with limit
            var notifications = await GetAllAsync(
                predicate,
                q => q.OrderByDescending(n => n.CreatedAt),
                n => n.Sender,
                n => n.Receiver);

            // Map to response DTOs and apply limit
            var mappedNotifications = _mapper.Map<List<UserNotificationResponse>>(notifications);
            return mappedNotifications.Take(request.Count).ToList();
        }

        public async Task<List<AdminNotificationResponse>> GetNotificationsForAdminAsync(GetNotificationsRequest request)
        {
            // Build predicate for filtering
            Expression<Func<Notification, bool>> predicate = n => !n.IsDeleted;

            // Add additional filters
            if (request.IsRead.HasValue)
            {
                predicate = CombinePredicates(predicate, n => n.IsRead == request.IsRead.Value);
            }

            if (request.Type.HasValue)
            {
                predicate = CombinePredicates(predicate, n => n.Type == request.Type.Value);
            }

            // Get notifications with limit
            var notifications = await GetAllAsync(
                predicate,
                q => q.OrderByDescending(n => n.CreatedAt),
                n => n.Sender,
                n => n.Receiver);

            // Map to response DTOs and apply limit
            var mappedNotifications = _mapper.Map<List<AdminNotificationResponse>>(notifications);
            return mappedNotifications.Take(request.Count).ToList();
        }

        public async Task<Notification?> GetNotificationByIdAsync(Guid id)
        {
            return await GetByIdAsync(id, n => n.Sender, n => n.Receiver);
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var notification = await FirstOrDefaultAsync(
                n => !n.IsDeleted && n.Id == notificationId && 
                     (n.ReceiverId == userId || n.ReceiverId == null));

            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await CountAsync(n => !n.IsDeleted && 
                                        (n.ReceiverId == userId || n.ReceiverId == null) && 
                                        !n.IsRead);
        }

        public async Task<List<Notification>> GetNotificationsForUserAsync(Guid userId, int count = 10)
        {
            var notifications = await GetAllAsync(
                n => !n.IsDeleted && (n.ReceiverId == userId || n.ReceiverId == null),
                q => q.OrderByDescending(n => n.CreatedAt),
                n => n.Sender);
            
            return notifications.Take(count).ToList();
        }

        public async Task<List<Notification>> GetBroadcastNotificationsAsync(int count = 10)
        {
            var notifications = await GetAllAsync(
                n => !n.IsDeleted && n.ReceiverId == null,
                q => q.OrderByDescending(n => n.CreatedAt),
                n => n.Sender);
            
            return notifications.Take(count).ToList();
        }

        public async Task<bool> ExistsAsync(Guid notificationId)
        {
            return await AnyAsync(n => !n.IsDeleted && n.Id == notificationId);
        }

        /// <summary>
        /// Helper method to combine two predicates using AND logic
        /// </summary>
        private static Expression<Func<Notification, bool>> CombinePredicates(
            Expression<Func<Notification, bool>> predicate1,
            Expression<Func<Notification, bool>> predicate2)
        {
            var parameter = Expression.Parameter(typeof(Notification), "n");
            var left = Expression.Invoke(predicate1, parameter);
            var right = Expression.Invoke(predicate2, parameter);
            var combined = Expression.AndAlso(left, right);
            return Expression.Lambda<Func<Notification, bool>>(combined, parameter);
        }

    }
}
