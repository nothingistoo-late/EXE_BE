using System.Threading;
using System.Threading.Tasks;
using ChatBoxAI.Models;
using ChatBoxAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatBoxAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly IGeminiService _geminiService;

        public GeminiController(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<GenerateResponse>> Generate([FromBody] GenerateRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.Prompt))
            {
                return BadRequest("Prompt is required.");
            }

            var text = await _geminiService.GenerateTextAsync(request.Prompt, cancellationToken);
            return Ok(new GenerateResponse { Text = text });
        }
    }
}


