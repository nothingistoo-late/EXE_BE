using BusinessObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IWeeklyBlindBoxSubscriptionRepository : IGenericRepository<WeeklyBlindBoxSubscription, Guid>
    {
        Task<List<WeeklyBlindBoxSubscription>> GetByUserIdAsync(Guid userId);
        Task<WeeklyBlindBoxSubscription?> GetWithDetailsAsync(Guid subscriptionId);
    }
}

