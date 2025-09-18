using Repositories.WorkSeeds.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implements
{
    public class SubscriptionPackageRepository : GenericRepository<SubscriptionPackage, Guid>, ISubscriptionPackageRepository
    {
        public SubscriptionPackageRepository(EXE_BE context) : base(context)
        {
        }
    }
}
