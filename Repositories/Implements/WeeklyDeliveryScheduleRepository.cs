using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Implements
{
    public class WeeklyDeliveryScheduleRepository : GenericRepository<WeeklyDeliverySchedule, Guid>, IWeeklyDeliveryScheduleRepository
    {
        public WeeklyDeliveryScheduleRepository(EXE_BE context) : base(context)
        {
        }

        public async Task<WeeklyDeliverySchedule?> GetBySubscriptionAndWeekAsync(Guid subscriptionId, DateTime weekStartDate)
        {
            return await _dbSet
                .Include(d => d.Subscription)
                .FirstOrDefaultAsync(d => 
                    d.SubscriptionId == subscriptionId && 
                    d.WeekStartDate.Date == weekStartDate.Date);
        }

        public async Task<List<WeeklyDeliverySchedule>> GetBySubscriptionIdAsync(Guid subscriptionId)
        {
            return await _dbSet
                .Where(d => d.SubscriptionId == subscriptionId)
                .OrderBy(d => d.WeekStartDate)
                .ToListAsync();
        }

        public async Task<List<WeeklyDeliverySchedule>> GetPendingDeliveriesAsync(DateTime upToDate)
        {
            return await _dbSet
                .Include(d => d.Subscription)
                    .ThenInclude(s => s.User)
                .Include(d => d.Subscription)
                    .ThenInclude(s => s.BoxType)
                .Where(d => 
                    !d.IsPaused &&
                    ((d.FirstDeliveryDate.HasValue && d.FirstDeliveryDate <= upToDate && !d.IsFirstDelivered) ||
                     (d.SecondDeliveryDate.HasValue && d.SecondDeliveryDate <= upToDate && !d.IsSecondDelivered)))
                .ToListAsync();
        }

        public async Task<List<WeeklyDeliverySchedule>> GetPausedDeliveriesAsync()
        {
            return await _dbSet
                .Include(d => d.Subscription)
                    .ThenInclude(s => s.User)
                .Include(d => d.Subscription)
                    .ThenInclude(s => s.BoxType)
                .Where(d => 
                    d.IsPaused &&
                    d.Subscription.Status == WeeklySubscriptionStatus.Active &&
                    (!d.IsFirstDelivered || !d.IsSecondDelivered)) // Chưa giao đủ
                .OrderBy(d => d.WeekStartDate)
                .ToListAsync();
        }
    }
}

