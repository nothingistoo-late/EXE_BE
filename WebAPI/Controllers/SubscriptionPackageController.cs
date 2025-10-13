using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class SubscriptionPackageController : Controller
    {
        private readonly ISubscriptionPackageService _subscriptionPackageService;
        public SubscriptionPackageController(ISubscriptionPackageService subscriptionPackageService)
        {
            _subscriptionPackageService = subscriptionPackageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var result = await _subscriptionPackageService.GetAllAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
