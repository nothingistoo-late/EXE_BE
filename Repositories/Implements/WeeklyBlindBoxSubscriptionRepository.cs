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
    public class WeeklyBlindBoxSubscriptionRepository : GenericRepository<WeeklyBlindBoxSubscription, Guid>, IWeeklyBlindBoxSubscriptionRepository
    {
        public WeeklyBlindBoxSubscriptionRepository(EXE_BE context) : base(context)
        {
        }

        public async Task<List<WeeklyBlindBoxSubscription>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(s => s.BoxType)
                .Include(s => s.User)
                .Include(s => s.DeliverySchedules)
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }

        public async Task<WeeklyBlindBoxSubscription?> GetWithDetailsAsync(Guid subscriptionId)
        {
            return await _dbSet
                .Include(s => s.BoxType)
                .Include(s => s.User)
                .Include(s => s.DeliverySchedules.OrderBy(d => d.WeekStartDate))
                .FirstOrDefaultAsync(s => s.Id == subscriptionId && !s.IsDeleted);
        }
    }
}

