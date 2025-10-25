using DTOs.DiscountDTOs.Request;
using DTOs.DiscountDTOs.Respond;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IDiscountService
    {
        Task<ApiResult<DiscountRespondDTO>> GetDiscountByIdAsync(Guid id);
        Task<ApiResult<List<DiscountRespondDTO>>> GetAllDiscountsAsync();
        Task<ApiResult<DiscountRespondDTO>> CreateDiscountAsync(DiscountCreateDTO dto);
        Task<ApiResult<DiscountRespondDTO>> UpdateDiscountAsync(Guid id, DiscountUpdateDTO dto);
        Task<ApiResult<bool>> DeleteDiscountAsync(Guid id);
        Task<ApiResult<DiscountRespondDTO>> ValidateDiscountCodeAsync(string code);
    }
}
