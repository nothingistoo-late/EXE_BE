using Repositories.WorkSeeds.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implements
{
    public class HealthSurveyRepository : GenericRepository<HealthSurvey, Guid>, IHealthSurveyRepository
    {
        public HealthSurveyRepository(EXE_BE context) : base(context)
        {
        }
    }
}
 