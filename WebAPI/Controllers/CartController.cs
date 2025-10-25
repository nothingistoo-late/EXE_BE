using DTOs.CartDTOs.Request;
using DTOs.CartDTOs.Respond;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        /// <summary>
        /// Lấy thông tin giỏ hàng của user
        /// </summary>
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetCart(Guid userId)
        {
            var result = await _cartService.GetCartAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Lấy thông tin giỏ hàng của user với thông tin GiftBox
        /// </summary>
        [HttpGet("{userId:guid}/with-giftbox")]
        public async Task<IActionResult> GetCartWithGiftBox(Guid userId)
        {
            var result = await _cartService.GetCartWithGiftBoxAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Thêm 1 item vào giỏ hàng
        /// </summary>
        [HttpPost("{userId:guid}/items")]
        public async Task<IActionResult> AddItem(Guid userId, [FromBody] AddItemDto dto)
        {
            var result = await _cartService.AddItemAsync(userId, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Cập nhật số lượng của 1 item trong giỏ hàng
        /// </summary>
        [HttpPut("{userId:guid}/items/{orderDetailId:guid}")]
        public async Task<IActionResult> UpdateItemQuantity(Guid userId, Guid orderDetailId, [FromQuery] int quantity)
        {
            var result = await _cartService.UpdateItemQuantityAsync(userId, orderDetailId, quantity);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Xoá 1 item khỏi giỏ hàng
        /// </summary>
        [HttpDelete("{userId:guid}/items/{orderDetailId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid userId, Guid orderDetailId)
        {
            var result = await _cartService.RemoveItemAsync(userId, orderDetailId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Xoá toàn bộ item trong giỏ hàng
        /// </summary>
        [HttpDelete("{userId:guid}")]
        public async Task<IActionResult> ClearCart(Guid userId)
        {
            var result = await _cartService.ClearCartAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Tiến hành checkout giỏ hàng
        /// </summary>
        [HttpPost("{userId:guid}/checkout")]
        public async Task<IActionResult> Checkout(Guid userId, [FromBody] CheckoutDto dto)
        {
            var result = await _cartService.CheckoutAsync(userId, dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
