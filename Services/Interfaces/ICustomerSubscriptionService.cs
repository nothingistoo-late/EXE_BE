using DTOs.CustomerSubscriptionRequest.Request;
using DTOs.CustomerSubscriptionRequest.Respond;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ICustomerSubscriptionService
    {
        Task<ApiResult<CustomerSubscriptionResponse>> PurchaseSubscriptionAsync(CustomerPurchaseSubscriptionRequest request);
        Task<ApiResult<CustomerSubscriptionResponse>> GetCustomerSubscriptionsAsync(Guid customerId);
        Task<ApiResult<CustomerSubscriptionResponse>> UpdateStatusSubscriptionAsync(Guid subscriptionId);
        Task<ApiResult<CustomerSubscriptionResponse>> GetSubscriptionByIdAsync(Guid subscriptionId);
        Task<ApiResult<List<CustomerSubscriptionResponse>>> GetAllSubscriptionsAsync();
        Task<ApiResult<List<MarkPaidSubscriptionResult>>> MarkPaidSubscriptionsAsync(List<Guid> subscriptionIds);
        Task<ApiResult<List<MarkPaidSubscriptionResult>>> MarkUnpaidSubscriptionsAsync(List<Guid> subscriptionIds);

    }
}
