using Microsoft.EntityFrameworkCore;
using Repositories.WorkSeeds.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implements
{
    public class CustomerSubscriptionRepository : GenericRepository<CustomerSubscription, Guid>, ICustomerSubscriptionRepository
    {
        public CustomerSubscriptionRepository(EXE_BE context) : base(context)
        {
        }

        public async Task<IEnumerable<CustomerSubscription>> GetByCustomerIdAsync(Guid customerId)
        {
            return await _dbSet
                .Include(x => x.SubscriptionPackage)
                .Where(x => x.CustomerId == customerId)
                .ToListAsync();
        }
    }
}
