using DTOs.CustomerSubscriptionRequest.Request;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerSubscriptionController : Controller
    {

        private readonly ICustomerSubscriptionService _service;

        public CustomerSubscriptionController(ICustomerSubscriptionService service)
        {
            _service = service;
        }

        [HttpPost("purchase")]
        public async Task<IActionResult> Purchase([FromBody] CustomerPurchaseSubscriptionRequest request)
        {
            var result = await _service.PurchaseSubscriptionAsync(request);
            if (!result.IsSuccess)
                return BadRequest(result);
            return Ok(result);
        }

        // Lấy tất cả subscriptions
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllSubscriptionsAsync();
            return Ok(result);
        }

        // Lấy danh sách subscription của 1 customer
        [HttpGet("customer/{customerId:guid}")]
        public async Task<IActionResult> GetByCustomer(Guid customerId)
        {
            var result = await _service.GetCustomerSubscriptionsAsync(customerId);
            return Ok(result);
        }

        // Lấy subscription theo Id
        [HttpGet("{subscriptionId:guid}")]
        public async Task<IActionResult> GetById(Guid subscriptionId)
        {
            var result = await _service.GetSubscriptionByIdAsync(subscriptionId);
            return Ok(result);
        }

        // Cập nhật trạng thái
        [HttpPut("{subscriptionId:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid subscriptionId)
        {
            var result = await _service.UpdateStatusSubscriptionAsync(subscriptionId);
            return Ok(result);
        }

        [HttpPut("mark-paid")]
        public async Task<IActionResult> MarkPaid([FromBody] List<Guid> subscriptionIds)
        {
            var result = await _service.MarkPaidSubscriptionsAsync(subscriptionIds);
            return Ok(result);
        }

        [HttpPut("mark-unpaid")]
        public async Task<IActionResult> MarkUnPaid([FromBody] List<Guid> subscriptionIds)
        {
            var result = await _service.MarkUnpaidSubscriptionsAsync(subscriptionIds);
            return Ok(result);
        }

    }
}
