using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : Controller
    {
        private readonly IStatisticsService _statisticsService;
        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }
        [HttpGet]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var request = new OrderStatisticsRequest
            {
                StartDate = start,
                EndDate = end
            };

            var result = await _statisticsService.GetOrderStatisticsAsync(request);

            return Ok(result);
        }
    } 
}
