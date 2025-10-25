using DTOs.OrderDTOs.Request;
using DTOs.OrderDTOs.Respond;
using DTOs.PayOSDTOs;
using Services.Commons;
using BusinessObjects.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResult<OrderResponse>> CreateOrderAsync(CreateOrderRequest request);
        Task<ApiResult<OrderResponse>> GetOrderByIdAsync(Guid id);
        Task<ApiResult<OrderResponseWithGiftBox>> GetOrderWithGiftBoxByIdAsync(Guid id);
        Task<ApiResult<List<OrderResponseWithGiftBox>>> GetOrdersWithGiftBoxByUserIdAsync(Guid userId);
        Task<ApiResult<List<OrderResponseWithGiftBox>>> GetAllOrdersWithGiftBoxForAdminAsync();
        Task<ApiResult<OrderResponse>> GetOrderByIdForDebugAsync(Guid orderId);
        Task<ApiResult<OrderResponse>> FindOrderByPayOSOrderCodeAsync(string orderCode);
        Task<ApiResult<bool>> VerifyOrderPaymentFlowAsync(Guid orderId);
        Task<ApiResult<List<OrderResponse>>> GetAllOrderAsync();
        Task<ApiResult<List<OrderResponse>>> GetAllOrdersByCustomerIDAsync(Guid customerId);
        Task<ApiResult<OrderResponse>> CancelledOrderAsync(Guid id);
        Task<ApiResult<List<UpdateOrderStatusResult>>> UpdateOrderStatusAsync(List<Guid> guids, OrderStatus status);
        Task<ApiResult<bool>> UpdateOrderStatusByOrderCodeAsync(string orderCode, OrderStatus status, object? paymentInfo = null);
        Task<ApiResult<PaymentLinkResponse>> CreatePayOSPaymentAsync(Guid orderId);
        Task<ApiResult<bool>> ProcessPayOSPaymentAsync(string paymentLinkId, string orderCode);
        
        /// <summary>
        /// Tạo gói hàng tuần - 2 đơn hàng với giá ưu đãi 250k thay vì 300k
        /// </summary>
        Task<ApiResult<WeeklyPackageResponse>> CreateWeeklyPackageAsync(CreateWeeklyPackageRequest request);
    }
}
