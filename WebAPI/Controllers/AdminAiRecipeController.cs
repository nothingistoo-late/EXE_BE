using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using DTOs.AiMenuDTOs.Request;
using DTOs.AiMenuDTOs.Response;
using BusinessObjects.Common;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/ai-recipes")]
    [Authorize(Roles = "ADMIN")] // Assuming you have role-based authorization
    public class AdminAiRecipeController : ControllerBase
    {
        private readonly IAiMenuService _aiMenuService;
        private readonly ILogger<AdminAiRecipeController> _logger;

        public AdminAiRecipeController(IAiMenuService aiMenuService, ILogger<AdminAiRecipeController> logger)
        {
            _aiMenuService = aiMenuService;
            _logger = logger;
        }

        /// <summary>
        /// Generate AI recipe for a specific date
        /// </summary>
        /// <param name="request">Request containing ingredients and date</param>
        /// <returns>Generated recipe for the specified date</returns>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRecipe([FromBody] AdminGenerateRecipeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _aiMenuService.GenerateRecipeForDateAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateRecipe endpoint for date {Date}", request.Date);
                return StatusCode(500, new ApiResult<AdminGenerateRecipeResponse>
                {
                    IsSuccess = false,
                    Message = "An error occurred while generating the recipe",
                    Exception = ex
                });
            }
        }
    }
}
