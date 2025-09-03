using Repositories.WorkSeeds.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implements
{
    public class DiscountRepository : GenericRepository<Discount, Guid>, IDiscountRepository
    {
        public DiscountRepository(EXE_BE context) : base(context)
        {
        }
    }
}
