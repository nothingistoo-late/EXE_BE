using BusinessObjects.Common;
using DTOs.AiMenuDTOs.Request;
using DTOs.AiMenuDTOs.Response;

namespace Services.Interfaces
{
    public interface IAiMenuService
    {
        Task<ApiResult<GenerateRecipeResponse>> GenerateRecipesAsync(GenerateRecipeRequest request);
        Task<ApiResult<GetUserRecipesResponse>> GetUserRecipesAsync(GetUserRecipesRequest request);
        Task<ApiResult<AiRecipeResponse>> GetRecipeByIdAsync(Guid recipeId);
        Task<ApiResult<List<AiRecipeResponse>>> GetRecentRecipesAsync(int count = 5);
        Task<ApiResult<List<AiRecipeResponse>>> GetRecipesByVegetablesAsync(List<string> vegetables, int count = 5);
        Task<ApiResult<bool>> DeleteRecipeAsync(Guid recipeId);
        Task<ApiResult<int>> GetUserRecipeCountAsync();
    }
}
