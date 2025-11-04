using DTOs.OrderDTOs.Request;
using DTOs.OrderDTOs.Respond;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using BusinessObjects.Common;
using System.Linq;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Create new order
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Dữ liệu không hợp lệ",
                    Data = errors
                });
            }

            var result = await _orderService.CreateOrderAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var result = await _orderService.GetOrderByIdAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Get order by ID with GiftBox information
        /// </summary>
        [HttpGet("{id:guid}/with-giftbox")]
        public async Task<IActionResult> GetOrderWithGiftBoxById(Guid id)
        {
            var result = await _orderService.GetOrderWithGiftBoxByIdAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Get all orders
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]

        public async Task<IActionResult> GetAllOrders()
        {
            var result = await _orderService.GetAllOrderAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get all orders by customer id
        /// </summary>
        [HttpGet("customer/{customerId:guid}")]
        public async Task<IActionResult> GetAllOrdersByCustomerId(Guid customerId)
        {
            var result = await _orderService.GetAllOrdersByCustomerIDAsync(customerId);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Get all orders with GiftBox info by user id
        /// </summary>
        [HttpGet("user/{userId:guid}/with-giftbox")]
        public async Task<IActionResult> GetOrdersWithGiftBoxByUserId(Guid userId)
        {
            var result = await _orderService.GetOrdersWithGiftBoxByUserIdAsync(userId);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Get all orders with GiftBox info for admin management
        /// </summary>
        [HttpGet("admin/with-giftbox")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAllOrdersWithGiftBoxForAdmin()
        {
            var result = await _orderService.GetAllOrdersWithGiftBoxForAdminAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Cancel order by ID
        /// </summary>
        [HttpPut("{id:guid}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var result = await _orderService.CancelledOrderAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        /// <summary>
        /// Batch update order status
        /// </summary>
        [HttpPut("status")]
        [Authorize(Roles = "ADMIN")]

        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            var result = await _orderService.UpdateOrderStatusAsync(request.OrderIds, request.Status);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Create PayOS payment link for order
        /// </summary>
        [HttpPost("{orderId:guid}/payos/payment-link")]
        public async Task<IActionResult> CreatePayOSPaymentLink(Guid orderId)
        {
            var result = await _orderService.CreatePayOSPaymentAsync(orderId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Create weekly package - 2 orders with discounted price (250k instead of 300k)
        /// Orders are delivered 3 days apart
        /// </summary>
        [HttpPost("weekly")]
        public async Task<IActionResult> CreateWeeklyPackage([FromBody] CreateWeeklyPackageRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Dữ liệu không hợp lệ",
                    Data = errors
                });
            }

            var result = await _orderService.CreateWeeklyPackageAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
