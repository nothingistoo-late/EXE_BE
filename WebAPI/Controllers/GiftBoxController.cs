using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using DTOs.GiftBoxDTOs.Request;
using DTOs.GiftBoxDTOs.Response;
using BusinessObjects.Common;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GiftBoxController : ControllerBase
    {
        private readonly IGiftBoxService _giftBoxService;
        private readonly ILogger<GiftBoxController> _logger;

        public GiftBoxController(IGiftBoxService giftBoxService, ILogger<GiftBoxController> logger)
        {
            _giftBoxService = giftBoxService;
            _logger = logger;
        }

        /// <summary>
        /// Generate greeting message for gift box
        /// </summary>
        /// <param name="request">Request containing receiver, occasion, and wish details</param>
        /// <returns>Generated greeting message</returns>
        [HttpPost("generate-greeting")]
        public async Task<IActionResult> GenerateGreeting([FromBody] GenerateGreetingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _giftBoxService.GenerateGreetingAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateGreeting endpoint");
                return StatusCode(500, new ApiResult<GenerateGreetingResponse>
                {
                    IsSuccess = false,
                    Message = "An error occurred while generating greeting message",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Create a custom gift box order with selected vegetables and greeting message
        /// </summary>
        /// <param name="request">Request containing gift box customization details</param>
        /// <returns>Created gift box order</returns>
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateGiftBoxOrder([FromBody] CreateGiftBoxRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _giftBoxService.CreateGiftBoxOrderAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateGiftBoxOrder endpoint");
                return StatusCode(500, new ApiResult<GiftBoxOrderResponse>
                {
                    IsSuccess = false,
                    Message = "An error occurred while creating gift box order",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Add a custom gift box to cart with selected vegetables and greeting message
        /// </summary>
        /// <param name="request">Request containing gift box customization details for cart</param>
        /// <returns>Added gift box to cart</returns>
        [HttpPost("add-to-cart")]
        public async Task<IActionResult> AddGiftBoxToCart([FromBody] AddGiftBoxToCartRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _giftBoxService.AddGiftBoxToCartAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddGiftBoxToCart endpoint");
                return StatusCode(500, new ApiResult<AddGiftBoxToCartResponse>
                {
                    IsSuccess = false,
                    Message = "An error occurred while adding gift box to cart",
                    Exception = ex
                });
            }
        }
    }
}

