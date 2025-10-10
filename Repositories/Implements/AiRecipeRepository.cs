using AutoMapper;
using BusinessObjects;
using DTOs.AiMenuDTOs.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;
using System.Linq.Expressions;

namespace Repositories.Implements
{
    public class AiRecipeRepository : GenericRepository<AiRecipe, Guid>, IAiRecipeRepository
    {
        private readonly IMapper _mapper;

        public AiRecipeRepository(EXE_BE context, IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public async Task<List<AiRecipeResponse>> GetUserRecipesAsync(
            Guid userId,
            int count,
            string? searchTerm = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            // Build predicate for filtering
            Expression<Func<AiRecipe, bool>> predicate = r => r.UserId == userId && !r.IsDeleted;

            // Add additional filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                predicate = CombinePredicates(predicate, r => r.DishName.Contains(searchTerm));
            }

            if (fromDate.HasValue)
            {
                predicate = CombinePredicates(predicate, r => r.GeneratedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                predicate = CombinePredicates(predicate, r => r.GeneratedAt <= toDate.Value);
            }

            // Get recipes with limit
            var recipes = await GetAllAsync(
                predicate,
                q => q.OrderByDescending(r => r.GeneratedAt));

            // Map to response DTOs and apply limit
            var mappedRecipes = _mapper.Map<List<AiRecipeResponse>>(recipes);
            return mappedRecipes.Take(count).ToList();
        }

        public async Task<List<AiRecipeResponse>> GetRecentRecipesAsync(Guid userId, int count = 5)
        {
            var recipes = await GetAllAsync(
                r => r.UserId == userId && !r.IsDeleted,
                q => q.OrderByDescending(r => r.GeneratedAt));

            var mappedRecipes = _mapper.Map<List<AiRecipeResponse>>(recipes);
            return mappedRecipes.Take(count).ToList();
        }

        public async Task<bool> UserHasRecipesAsync(Guid userId)
        {
            return await AnyAsync(r => r.UserId == userId && !r.IsDeleted);
        }

        public async Task<int> GetUserRecipeCountAsync(Guid userId)
        {
            return await CountAsync(r => r.UserId == userId && !r.IsDeleted);
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

        /// <summary>
        /// Helper method to combine two predicates using AND logic
        /// </summary>
        private static Expression<Func<AiRecipe, bool>> CombinePredicates(
            Expression<Func<AiRecipe, bool>> predicate1,
            Expression<Func<AiRecipe, bool>> predicate2)
        {
            var parameter = Expression.Parameter(typeof(AiRecipe), "r");
            var left = Expression.Invoke(predicate1, parameter);
            var right = Expression.Invoke(predicate2, parameter);
            var combined = Expression.AndAlso(left, right);
            return Expression.Lambda<Func<AiRecipe, bool>>(combined, parameter);
        }
    }
}
