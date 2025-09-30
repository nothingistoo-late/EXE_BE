using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Services.Interfaces;

namespace Services.Implementations
{
    public class GroqAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public GroqAIService(HttpClient httpClient, string apiKey, string model = "llama-3.1-8b-instant")
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _model = model;
            
            // Set default headers
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Groq API key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
            }

            var request = new GroqRequest
            {
                Messages = new[]
                {
                    new GroqMessage
                    {
                        Role = "user",
                        Content = prompt
                    }
                },
                Model = _model,
                MaxTokens = 1000,
                Temperature = 0.7
            };

            var uri = "https://api.groq.com/openai/v1/chat/completions";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = JsonContent.Create(request, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };

            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Groq API error {(int)response.StatusCode}: {response.ReasonPhrase}. Body: {errorBody}");
            }

            var payload = await response.Content.ReadFromJsonAsync<GroqResponse>(jsonOptions, cancellationToken);
            if (payload?.Choices == null || payload.Choices.Length == 0)
            {
                return "Xin lỗi, tôi không thể tạo phản hồi lúc này.";
            }

            var result = payload.Choices[0];
            return result.Message?.Content ?? "Xin lỗi, tôi không thể tạo phản hồi lúc này.";
        }

        public async Task<string> ChatAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Groq API key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));
            }

            var request = new GroqRequest
            {
                Messages = new[]
                {
                    new GroqMessage
                    {
                        Role = "user",
                        Content = message
                    }
                },
                Model = _model,
                MaxTokens = 1500,
                Temperature = 0.8
            };

            var uri = "https://api.groq.com/openai/v1/chat/completions";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = JsonContent.Create(request, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };

            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Groq API error {(int)response.StatusCode}: {response.ReasonPhrase}. Body: {errorBody}");
            }

            var payload = await response.Content.ReadFromJsonAsync<GroqResponse>(jsonOptions, cancellationToken);
            if (payload?.Choices == null || payload.Choices.Length == 0)
            {
                return "Xin lỗi, tôi không thể trả lời lúc này.";
            }

            var result = payload.Choices[0];
            return result.Message?.Content ?? "Xin lỗi, tôi không thể trả lời lúc này.";
        }

        // DTOs for Groq API
        public class GroqRequest
        {
            public GroqMessage[] Messages { get; set; } = Array.Empty<GroqMessage>();
            public string Model { get; set; } = "llama-3.1-8b-instant";
            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; } = 1000;
            public double Temperature { get; set; } = 0.7;
        }

        public class GroqMessage
        {
            public string Role { get; set; } = "user";
            public string Content { get; set; } = string.Empty;
        }

        public class GroqResponse
        {
            public GroqChoice[]? Choices { get; set; }
        }

        public class GroqChoice
        {
            public GroqMessage? Message { get; set; }
        }
    }
}
