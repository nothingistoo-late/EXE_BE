using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using DTOs.AiMenuDTOs.Response;
using BusinessObjects.Common;
using Repositories.WorkSeeds.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/ai-recipes")]
    [Authorize] // Requires authentication but no specific role
    public class UserAiRecipeController : ControllerBase
    {
        private readonly IAiMenuService _aiMenuService;
        private readonly ICurrentTime _currentTime;
        private readonly ILogger<UserAiRecipeController> _logger;

        public UserAiRecipeController(IAiMenuService aiMenuService, ICurrentTime currentTime, ILogger<UserAiRecipeController> logger)
        {
            _aiMenuService = aiMenuService;
            _currentTime = currentTime;
            _logger = logger;
        }

        /// <summary>
        /// Get AI recipe for a specific date (defaults to today if no date provided)
        /// </summary>
        /// <param name="date">Date in yyyy-MM-dd format (optional, defaults to today)</param>
        /// <returns>Recipe for the specified date</returns>
        [HttpGet]
        public async Task<IActionResult> GetRecipeByDate([FromQuery] DateTime? date = null)
        {
            // Use today's date if no date provided
            var targetDate = date ?? _currentTime.GetVietnamTime().Date;
            
            try
            {
                var result = await _aiMenuService.GetRecipeByDateAsync(targetDate);
                
                if (!result.IsSuccess)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecipeByDate endpoint for date {Date}", targetDate);
                return StatusCode(500, new ApiResult<UserRecipeResponse>
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving the recipe",
                    Exception = ex
                });
            }
        }
    }
}
