using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class HealthSurvey : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid CustomerSubscriptionId { get; set; }
        public CustomerSubscription CustomerSubscription { get; set; }
        public string Allergy { get; set; } 
        public string Feeling { get; set; }
    }
}
