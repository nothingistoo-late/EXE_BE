using DTOs.GiftBoxDTOs.Request;
using DTOs.GiftBoxDTOs.Response;
using Services.Commons;

namespace Services.Interfaces
{
    public interface IGiftBoxService
    {
        Task<ApiResult<GenerateGreetingResponse>> GenerateGreetingAsync(GenerateGreetingRequest request);
        Task<ApiResult<GiftBoxOrderResponse>> CreateGiftBoxOrderAsync(CreateGiftBoxRequest request);
    }
}

