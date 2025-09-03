using DTOs.OrderDTOs.Request;
using DTOs.OrderDTOs.Respond;
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
        Task<ApiResult<List<OrderResponse>>> GetAllOrderAsync();
        Task<ApiResult<List<OrderResponse>>> GetAllOrdersByCustomerIDAsync(Guid customerId);
        Task<ApiResult<OrderResponse>> CancelledOrderAsync(Guid id);
    }
}
