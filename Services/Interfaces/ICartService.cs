using DTOs.CartDTOs.Request;
using DTOs.CartDTOs.Respond;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ICartService
    {
        Task<ApiResult<CartResponse>> GetCartAsync(Guid userId);
        Task<ApiResult<CartResponseWithGiftBox>> GetCartWithGiftBoxAsync(Guid userId);
        Task<ApiResult<CartResponse>> AddItemAsync(Guid userId, AddItemDto dto);
        Task<ApiResult<CartResponse>> UpdateItemQuantityAsync(Guid userId, Guid orderDetailId, int quantity);
        Task<ApiResult<bool>> RemoveItemAsync(Guid userId, Guid orderDetailId);
        Task<ApiResult<bool>> ClearCartAsync(Guid userId);
        Task<ApiResult<CartResponse>> CheckoutAsync(Guid userId, CheckoutDto dto);
    }
}
