using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatBoxAI.Options;
using Microsoft.Extensions.Options;

namespace ChatBoxAI.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        public GeminiService(HttpClient httpClient, IOptions<GeminiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Gemini ApiKey is not configured.");
            }

            var model = string.IsNullOrWhiteSpace(_options.Model)
                ? "gemini-1.5-flash"
                : _options.Model;

            var request = new GeminiGenerateContentRequest
            {
                Contents =
                {
                    new GeminiContent
                    {
                        Role = "user",
                        Parts = { new GeminiPart { Text = prompt } }
                    }
                }
            };

            // Endpoint: https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key=API_KEY
            var uri = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_options.ApiKey}";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = JsonContent.Create(request, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };

            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Gemini API error {(int)response.StatusCode}: {response.ReasonPhrase}. Body: {errorBody}");
            }

            var payload = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(jsonOptions, cancellationToken);
            if (payload == null)
            {
                return string.Empty;
            }

            var candidate = payload.Candidates?.FirstOrDefault();
            var text = candidate?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
            return text;
        }

        // DTOs compatible with Gemini API
        public class GeminiGenerateContentRequest
        {
            public System.Collections.Generic.List<GeminiContent> Contents { get; set; } = new();
        }

        public class GeminiContent
        {
            public string Role { get; set; } = "user";
            public System.Collections.Generic.List<GeminiPart> Parts { get; set; } = new();
        }

        public class GeminiPart
        {
            public string Text { get; set; } = string.Empty;
        }

        public class GeminiGenerateContentResponse
        {
            public System.Collections.Generic.List<GeminiCandidate>? Candidates { get; set; }
        }

        public class GeminiCandidate
        {
            public GeminiContent? Content { get; set; }
        }
    }
}


