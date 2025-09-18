using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface ICustomerSubscriptionRepository : IGenericRepository<CustomerSubscription, Guid>
    {
        Task<IEnumerable<CustomerSubscription>> GetByCustomerIdAsync(Guid customerId);

    }
}
