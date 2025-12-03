using DTOs.Gemini;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;

        public AIController(IAIService aiService)
        {
            _aiService = aiService;
        }

        /// <summary>
        /// Generate text using AI
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult<GenerateResponseDTO>> Generate([FromBody] GenerateRequestDTO request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.Prompt))
            {
                return BadRequest("Prompt is required.");
            }

            try
            {
                var text = await _aiService.GenerateTextAsync(request.Prompt, cancellationToken);
                return Ok(new GenerateResponseDTO { Text = text });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating text: {ex.Message}");
            }
        }

        /// <summary>
        /// Chat with AI
        /// </summary>
        [AllowAnonymous]
        [HttpPost("chat")]
        public async Task<ActionResult<ChatResponseDTO>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest("Message is required.");
            }

            try
            {
                var response = await _aiService.ChatAsync(request.Message, cancellationToken);
                return Ok(new ChatResponseDTO { Response = response });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error in chat: {ex.Message}");
            }
        }
        [HttpPost("generate-wish")]
        public async Task<IActionResult> GenerateWish([FromBody] GenerateWishRequestDto request, CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest("Request body cannot be empty.");

            if (string.IsNullOrWhiteSpace(request.Receiver) ||
                string.IsNullOrWhiteSpace(request.Occasion) ||
                string.IsNullOrWhiteSpace(request.MainWish))
            {
                return BadRequest("Receiver, Occasion, and MainWish are required.");
            }

            var result = await _aiService.GenerateWishAsync(
                request.Receiver,
                request.Occasion,
                request.MainWish,
                request.Custom,
                cancellationToken);

            return Ok(new { wish = result });
        }
    }

    // DTOs
    public class GenerateRequestDTO
    {
        public string Prompt { get; set; } = string.Empty;
    }

    public class GenerateResponseDTO
    {
        public string Text { get; set; } = string.Empty;
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponseDTO
    {
        public string Response { get; set; } = string.Empty;
    }
}

