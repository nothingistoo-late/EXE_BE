using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

