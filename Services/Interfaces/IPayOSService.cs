using DTOs.Options;
using DTOs.PayOSDTOs;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Services.Commons;
using BusinessObjects.Common;

namespace Services.Interfaces
{
    public interface IPayOSService
    {
        Task<ApiResult<PaymentLinkResponse>> CreatePaymentLinkAsync(CreatePaymentLinkRequest request);
        Task<ApiResult<object>> GetPaymentInformationAsync(string paymentLinkId);
        Task<ApiResult<bool>> CancelPaymentLinkAsync(string paymentLinkId);
        Task<bool> VerifyWebhookDataAsync(PayOSWebhookData webhookData);
    }
}
