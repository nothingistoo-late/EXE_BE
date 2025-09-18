using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs.CustomerSubscriptionRequest.Respond
{
    public class MarkPaidSubscriptionResult
    {
        public Guid SubscriptionId { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
