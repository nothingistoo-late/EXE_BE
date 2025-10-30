using BusinessObjects;
using System;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IWeeklyDeliveryScheduleRepository : IGenericRepository<WeeklyDeliverySchedule, Guid>
    {
        Task<WeeklyDeliverySchedule?> GetBySubscriptionAndWeekAsync(Guid subscriptionId, DateTime weekStartDate);
        Task<List<WeeklyDeliverySchedule>> GetBySubscriptionIdAsync(Guid subscriptionId);
        Task<List<WeeklyDeliverySchedule>> GetPendingDeliveriesAsync(DateTime upToDate);
        Task<List<WeeklyDeliverySchedule>> GetPausedDeliveriesAsync();
    }
}

