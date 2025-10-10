using BusinessObjects;
using DTOs.AiMenuDTOs.Response;
using Repositories.WorkSeeds.Interfaces;

namespace Repositories.Interfaces
{
    public interface IAiRecipeRepository : IGenericRepository<AiRecipe, Guid>
    {
        Task<List<AiRecipeResponse>> GetUserRecipesAsync(
            Guid userId,
            int count,
            string? searchTerm = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        Task<List<AiRecipeResponse>> GetRecentRecipesAsync(Guid userId, int count = 5);

        Task<bool> UserHasRecipesAsync(Guid userId);

        Task<int> GetUserRecipeCountAsync(Guid userId);

        Task<List<AiRecipeResponse>> GetRecipesByVegetablesAsync(
            Guid userId,
            List<string> vegetables,
            int count = 5);
    }
}
