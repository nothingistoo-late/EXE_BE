using AutoMapper;
using BusinessObjects;
using DTOs.AiMenuDTOs.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implements
{
    public class AiRecipeRepository : GenericRepository<AiRecipe, Guid>, IAiRecipeRepository
    {
        private readonly IMapper _mapper;

        public AiRecipeRepository(EXE_BE context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<PagedList<AiRecipeResponse>> GetUserRecipesAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.AiRecipes
                .Where(r => r.UserId == userId && !r.IsDeleted);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.DishName.Contains(searchTerm));
            }

            // Apply date filters
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.GeneratedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.GeneratedAt <= toDate.Value);
            }

            // Order by most recent first
            query = query.OrderByDescending(r => r.GeneratedAt);

            var totalCount = await query.CountAsync();
            var recipes = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var recipeResponses = _mapper.Map<List<AiRecipeResponse>>(recipes);
            return new PagedList<AiRecipeResponse>(recipeResponses, totalCount, pageNumber, pageSize);
        }

        public async Task<List<AiRecipeResponse>> GetRecentRecipesAsync(Guid userId, int count = 5)
        {
            var recipes = await _context.AiRecipes
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .OrderByDescending(r => r.GeneratedAt)
                .Take(count)
                .ToListAsync();

            return _mapper.Map<List<AiRecipeResponse>>(recipes);
        }

        public async Task<bool> UserHasRecipesAsync(Guid userId)
        {
            return await _context.AiRecipes
                .AnyAsync(r => r.UserId == userId && !r.IsDeleted);
        }

        public async Task<int> GetUserRecipeCountAsync(Guid userId)
        {
            return await _context.AiRecipes
                .CountAsync(r => r.UserId == userId && !r.IsDeleted);
        }

        public async Task<List<AiRecipeResponse>> GetRecipesByVegetablesAsync(
            Guid userId,
            List<string> vegetables,
            int count = 5)
        {
            var recipes = await _context.AiRecipes
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .ToListAsync();

            // Filter recipes that contain any of the specified vegetables
            var matchingRecipes = recipes
                .Where(r =>
                {
                    var inputVeggies = JsonConvert.DeserializeObject<List<string>>(r.InputVegetables) ?? new List<string>();
                    return inputVeggies.Any(v => vegetables.Any(veg => 
                        v.Contains(veg, StringComparison.OrdinalIgnoreCase) || 
                        veg.Contains(v, StringComparison.OrdinalIgnoreCase)));
                })
                .OrderByDescending(r => r.GeneratedAt)
                .Take(count)
                .ToList();

            return _mapper.Map<List<AiRecipeResponse>>(matchingRecipes);
        }
    }
}
