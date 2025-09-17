using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatBoxAI.Models
{
    public class GenerateRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }

    public class GenerateResponse
    {
        public string Text { get; set; } = string.Empty;
    }

    // Minimal DTOs matching Gemini REST API structure for text generation
    internal class GeminiGenerateContentRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();
    }

    internal class GeminiContent
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    internal class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    internal class GeminiGenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate> Candidates { get; set; } = new();
    }

    internal class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();
    }
}


