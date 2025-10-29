using DTOs;
using DTOs.Customer.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IImageStorageService _imageStorage;

        public CustomersController(ICustomerService customerService, IImageStorageService imageStorage)
        {
            _customerService = customerService;
            _imageStorage = imageStorage;
        }

        /// <summary>
        /// Lấy tất cả khách hàng (chưa bị xóa mềm)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAllCustomers()
        {
            var result = await _customerService.GetAllCustomersAsync();
            if (!result.IsSuccess)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới khách hàng (User + Customer)
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequestDTO dto)
        {
            var result = await _customerService.CreateCustomerAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(result);
            return Ok(result);
        }
        /// <summary>
        /// Lấy thông tin khách hàng theo Id
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "ADMIN")]

        public async Task<IActionResult> GetCustomerById(Guid id)
        {
            var result = await _customerService.GetCustomerByIdAsync(id);
            if (!result.IsSuccess)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Xóa mềm khách hàng
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]

        public async Task<IActionResult> SoftDeleteCustomer(Guid id)
        {
            var result = await _customerService.SoftDeleteCustomerById(id);
            if (!result.IsSuccess)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin cá nhân của customer hiện tại (dựa trên token)
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var result = await _customerService.GetMyProfileAsync();

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin profile (chỉ sửa những trường được truyền lên)
        /// </summary>
        [HttpPut("me")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateMyProfile([FromForm] UpdateMyProfileRequest request, IFormFile? avatar)
        {
            // Nếu có ảnh upload thì lưu vào wwwroot/uploads/avatars và gán ImgURL
            if (avatar != null && avatar.Length > 0)
            {
                var uploadedUrl = await _imageStorage.UploadAsync(avatar);
                request.ImgURL = uploadedUrl;
            }

            var result = await _customerService.UpdateMyProfileAsync(request);

            if (!result.IsSuccess)
                return BadRequest(result);
            return Ok(result); // trả về ApiResult<MyProfileResponse>
        }
    }
}
