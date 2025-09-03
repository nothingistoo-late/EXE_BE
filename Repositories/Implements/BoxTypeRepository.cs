using Repositories.WorkSeeds.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implements
{
    public class BoxTypeRepository : GenericRepository<BoxTypes, Guid>, IBoxTypeRepository
    {
        public BoxTypeRepository(EXE_BE context) : base(context)
        {
        }
    }
}
