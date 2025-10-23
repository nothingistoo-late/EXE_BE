using DTOs.ReviewDTOs.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        /// <summary>
        /// Create a new review
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
        {
            var userId = Guid.Parse(User.FindFirst("UserId")?.Value ?? Guid.Empty.ToString());
            var result = await _reviewService.CreateReviewAsync(request, userId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Update an existing review
        /// </summary>
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateReview([FromBody] UpdateReviewRequest request)
        {
            var userId = Guid.Parse(User.FindFirst("UserId")?.Value ?? Guid.Empty.ToString());
            var result = await _reviewService.UpdateReviewAsync(request, userId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get review by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReviewById(Guid id)
        {
            var result = await _reviewService.GetReviewByIdAsync(id);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }

        /// <summary>
        /// Get detailed review information
        /// </summary>
        [HttpGet("{id}/detail")]
        public async Task<IActionResult> GetReviewDetail(Guid id)
        {
            var result = await _reviewService.GetReviewDetailAsync(id);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }

        /// <summary>
        /// Get review by order ID
        /// </summary>
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetReviewByOrderId(Guid orderId)
        {
            var result = await _reviewService.GetReviewByOrderIdAsync(orderId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }

        /// <summary>
        /// Get reviews with pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetReviewsWithPagination([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var result = await _reviewService.GetReviewsWithPaginationAsync(pageNumber, pageSize);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get review statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetReviewStatistics()
        {
            var result = await _reviewService.GetReviewStatisticsAsync();
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get review statistics by box type
        /// </summary>
        [HttpGet("statistics/box-type/{boxTypeId}")]
        public async Task<IActionResult> GetReviewStatisticsByBoxType(Guid boxTypeId)
        {
            var result = await _reviewService.GetReviewStatisticsByBoxTypeAsync(boxTypeId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Delete a review
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst("UserId")?.Value ?? Guid.Empty.ToString());
            var result = await _reviewService.DeleteReviewAsync(id, userId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
    }
}

