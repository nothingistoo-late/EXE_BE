using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using DTOs.AiMenuDTOs.Request;
using DTOs.AiMenuDTOs.Response;
using BusinessObjects.Common;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AiMenuController : ControllerBase
    {
        private readonly IAiMenuService _aiMenuService;
        private readonly ILogger<AiMenuController> _logger;

        public AiMenuController(IAiMenuService aiMenuService, ILogger<AiMenuController> logger)
        {
            _aiMenuService = aiMenuService;
            _logger = logger;
        }

        /// <summary>
        /// Generate AI-powered recipes based on available vegetables
        /// </summary>
        /// <param name="request">Request containing vegetables and preferences</param>
        /// <returns>Generated recipes</returns>
        [HttpPost("generate-recipes")]
        public async Task<IActionResult> GenerateRecipes([FromBody] GenerateRecipeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _aiMenuService.GenerateRecipesAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateRecipes endpoint");
                return StatusCode(500, new ApiResult<GenerateRecipeResponse>
                {
                    IsSuccess = false,
                    Message = "An error occurred while generating recipes",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Get user's recipes with pagination and filtering
        /// </summary>
        /// <param name="request">Request containing pagination and filter parameters</param>
        /// <returns>Paginated list of user's recipes</returns>
        [HttpGet("my-recipes")]
        public async Task<IActionResult> GetUserRecipes([FromQuery] GetUserRecipesRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _aiMenuService.GetUserRecipesAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserRecipes endpoint");
                return StatusCode(500, new ApiResult<GetUserRecipesResponse>
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving recipes",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Get a specific recipe by ID
        /// </summary>
        /// <param name="recipeId">Recipe ID</param>
        /// <returns>Recipe details</returns>
        [HttpGet("recipes/{recipeId}")]
        public async Task<IActionResult> GetRecipeById(Guid recipeId)
        {
            try
            {
                var result = await _aiMenuService.GetRecipeByIdAsync(recipeId);
                
                if (!result.IsSuccess)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecipeById endpoint for recipe {RecipeId}", recipeId);
                return StatusCode(500, new ApiResult<AiRecipeResponse>
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving the recipe",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Get user's recent recipes
        /// </summary>
        /// <param name="count">Number of recent recipes to retrieve (default: 5)</param>
        /// <returns>List of recent recipes</returns>
        [HttpGet("recent-recipes")]
        public async Task<IActionResult> GetRecentRecipes([FromQuery] int count = 5)
        {
            try
            {
                if (count <= 0 || count > 20)
                {
                    return BadRequest(new ApiResult<List<AiRecipeResponse>>
                    {
                        IsSuccess = false,
                        Message = "Count must be between 1 and 20"
                    });
                }

                var result = await _aiMenuService.GetRecentRecipesAsync(count);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecentRecipes endpoint");
                return StatusCode(500, new ApiResult<List<AiRecipeResponse>>
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving recent recipes",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Get recipes based on specific vegetables
        /// </summary>
        /// <param name="vegetables">List of vegetables to search for</param>
        /// <param name="count">Number of recipes to retrieve (default: 5)</param>
        /// <returns>List of recipes containing the specified vegetables</returns>
        [HttpGet("recipes-by-vegetables")]
        public async Task<IActionResult> GetRecipesByVegetables(
            [FromQuery] List<string> vegetables, 
            [FromQuery] int count = 5)
        {
            try
            {
                if (!vegetables.Any())
                {
                    return BadRequest(new ApiResult<List<AiRecipeResponse>>
                    {
                        IsSuccess = false,
                        Message = "At least one vegetable is required"
                    });
                }

                if (count <= 0 || count > 20)
                {
                    return BadRequest(new ApiResult<List<AiRecipeResponse>>
                    {
                        IsSuccess = false,
                        Message = "Count must be between 1 and 20"
                    });
                }

                var result = await _aiMenuService.GetRecipesByVegetablesAsync(vegetables, count);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecipesByVegetables endpoint");
                return StatusCode(500, new ApiResult<List<AiRecipeResponse>>
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving recipes by vegetables",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Delete a recipe
        /// </summary>
        /// <param name="recipeId">Recipe ID to delete</param>
        /// <returns>Success status</returns>
        [HttpDelete("recipes/{recipeId}")]
        public async Task<IActionResult> DeleteRecipe(Guid recipeId)
        {
            try
            {
                var result = await _aiMenuService.DeleteRecipeAsync(recipeId);
                
                if (!result.IsSuccess)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteRecipe endpoint for recipe {RecipeId}", recipeId);
                return StatusCode(500, new ApiResult<bool>
                {
                    IsSuccess = false,
                    Message = "An error occurred while deleting the recipe",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Get user's total recipe count
        /// </summary>
        /// <returns>Total number of recipes for the current user</returns>
        [HttpGet("recipe-count")]
        public async Task<IActionResult> GetUserRecipeCount()
        {
            try
            {
                var result = await _aiMenuService.GetUserRecipeCountAsync();
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserRecipeCount endpoint");
                return StatusCode(500, new ApiResult<int>
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving recipe count",
                    Exception = ex
                });
            }
        }
    }
}
