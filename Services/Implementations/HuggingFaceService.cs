using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Services.Interfaces;
using Services.Options;

namespace Services.Implementations
{
    public class HuggingFaceService : IHuggingFaceService
    {
        private readonly HttpClient _httpClient;
        private readonly HuggingFaceOptions _options;

        public HuggingFaceService(HttpClient httpClient, IOptions<HuggingFaceOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
            
            // Set default headers
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        }

        public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Hugging Face ApiKey is not configured.");
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
            }

            // Sử dụng model miễn phí phổ biến
            var model = string.IsNullOrWhiteSpace(_options.Model)
                ? "facebook/blenderbot-400M-distill"
                : _options.Model;

            var request = new HuggingFaceRequest
            {
                Inputs = prompt,
                Parameters = new HuggingFaceParameters
                {
                    MaxLength = 100,
                    Temperature = 0.7,
                    DoSample = true
                }
            };

            var uri = $"https://api-inference.huggingface.co/models/{model}";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = JsonContent.Create(request, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };

            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Hugging Face API error {(int)response.StatusCode}: {response.ReasonPhrase}. Body: {errorBody}");
            }

            var payload = await response.Content.ReadFromJsonAsync<HuggingFaceResponse[]>(jsonOptions, cancellationToken);
            if (payload == null || !payload.Any())
            {
                return "Xin lỗi, tôi không thể tạo phản hồi lúc này.";
            }

            var result = payload.FirstOrDefault();
            return result?.GeneratedText ?? "Xin lỗi, tôi không thể tạo phản hồi lúc này.";
        }

        public async Task<string> ChatAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Hugging Face ApiKey is not configured.");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));
            }

            // Sử dụng model chat miễn phí
            var model = string.IsNullOrWhiteSpace(_options.Model)
                ? "facebook/blenderbot-400M-distill"
                : _options.Model;
            var request = new HuggingFaceChatRequest
            {
                Inputs = message,
                Parameters = new HuggingFaceParameters
                {
                    MaxLength = 150,
                    Temperature = 0.8,
                    DoSample = true,
                    ReturnFullText = false
                }
            };

            var uri = $"https://api-inference.huggingface.co/models/{model}";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = JsonContent.Create(request, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };

            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Hugging Face API error {(int)response.StatusCode}: {response.ReasonPhrase}. Body: {errorBody}");
            }

            var payload = await response.Content.ReadFromJsonAsync<HuggingFaceResponse[]>(jsonOptions, cancellationToken);
            if (payload == null || !payload.Any())
            {
                return "Xin lỗi, tôi không thể trả lời lúc này.";
            }

            var result = payload.FirstOrDefault();
            return result?.GeneratedText ?? "Xin lỗi, tôi không thể trả lời lúc này.";
        }

        // DTOs for Hugging Face API
        public class HuggingFaceRequest
        {
            public string Inputs { get; set; } = string.Empty;
            public HuggingFaceParameters? Parameters { get; set; }
        }

        public class HuggingFaceChatRequest
        {
            public string Inputs { get; set; } = string.Empty;
            public HuggingFaceParameters? Parameters { get; set; }
        }

        public class HuggingFaceParameters
        {
            public int MaxLength { get; set; } = 100;
            public double Temperature { get; set; } = 0.7;
            public bool DoSample { get; set; } = true;
            public bool ReturnFullText { get; set; } = false;
        }

        public class HuggingFaceResponse
        {
            public string GeneratedText { get; set; } = string.Empty;
        }
    }
}

