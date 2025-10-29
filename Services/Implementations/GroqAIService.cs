using System;
using System.Collections.Generic;
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

        public async Task<string> GenerateWishAsync(string Receiver, string occasion, string mainWish, string custom, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    throw new InvalidOperationException("Groq API key is not configured.");
                }

                if (string.IsNullOrWhiteSpace(Receiver) || string.IsNullOrWhiteSpace(occasion) || string.IsNullOrWhiteSpace(mainWish))
                {
                    throw new ArgumentException("Receiver, occasion, and mainWish cannot be null or empty.");
                }

                var customText = !string.IsNullOrWhiteSpace(custom) ? $"\n- Yêu cầu thêm: {custom}" : "";
                
                var prompt = $@"Viết ngay lời chúc dựa trên thông tin sau. CHỈ viết lời chúc, KHÔNG viết thêm bất kỳ câu nào khác (KHÔNG viết giải thích, KHÔNG viết 'Dưới đây là...', KHÔNG viết tiêu đề).

Thông tin:
- Người nhận: {Receiver}
- Dịp: {occasion}  
- Ý chính: {mainWish}{customText}

QUAN TRỌNG: 
- Bắt đầu trực tiếp bằng lời chúc
- Chỉ viết 3-5 câu chúc
- KHÔNG giải thích, KHÔNG mở đầu, KHÔNG kết thúc bằng câu ngoài lời chúc";

                var request = new GroqRequest
                {
                    Messages = new[]
                    {
                        new GroqMessage
                        {
                            Role = "system",
                            Content = "Bạn là một AI chuyên viết lời chúc ngắn gọn. Bạn CHỈ trả về lời chúc, không bao giờ viết giải thích hay câu dẫn nào khác."
                        },
                        new GroqMessage
                        {
                            Role = "user",
                            Content = prompt
                        }
                    },
                    Model = _model,
                    MaxTokens = 150,
                    Temperature = 0.3
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
                    return "Xin lỗi, tôi không thể tạo lời chúc lúc này.";
                }

                var result = payload.Choices[0];
                var rawContent = result.Message?.Content ?? "Xin lỗi, tôi không thể tạo lời chúc lúc này.";
                
                // Tự động tách lấy phần lời chúc thực sự, loại bỏ các câu giải thích
                return ExtractWishFromResponse(rawContent);
            }
            catch (Exception ex)
            {
                return $"Error generating wish: {ex.Message}";
            }
        }
        
        private string ExtractWishFromResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return response;

            // 1) Ưu tiên: lấy nội dung trong dấu ngoặc kép bằng Regex (hỗ trợ ", “ ”, ‘ ’)
            //    Lấy đoạn đầu tiên tìm được
            var quotePatterns = new[]
            {
                "\"(?<q>.+?)\"",
                "“(?<q>.+?)”",
                "‘(?<q>.+?)’",
                "'(?<q>.+?)'"
            };
            foreach (var pattern in quotePatterns)
            {
                var m = System.Text.RegularExpressions.Regex.Match(response, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
                if (m.Success)
                {
                    var q = m.Groups["q"].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(q))
                        return q;
                }
            }

            // 1b) Fallback: cắt từ dấu ngoặc kép đầu tiên tới dấu ngoặc kép đóng tương ứng
            int firstQuoteIdx = response.IndexOfAny(new[] { '"', '“', '‘', '\'' });
            if (firstQuoteIdx >= 0)
            {
                char open = response[firstQuoteIdx];
                char close = open == '“' ? '”' : open == '‘' ? '’' : open;
                int closeIdx = response.IndexOf(close, firstQuoteIdx + 1);
                if (closeIdx > firstQuoteIdx + 1)
                {
                    var between = response.Substring(firstQuoteIdx + 1, closeIdx - firstQuoteIdx - 1).Trim();
                    if (!string.IsNullOrWhiteSpace(between))
                        return between;
                }
            }

            // Các cụm từ giải thích cần bỏ
            var skipLinePrefixes = new[]
            {
                "Dưới đây là",
                "Đây là",
                "Hoặc một phiên bản",
                "Hoặc phiên bản",
                "Hoặc một lựa chọn",
                "Hoặc một lựa chọn khác",
                "Hoặc lựa chọn khác",
                "Một lời chúc",
                "Câu chúc",
                "Ví dụ",
                "Gợi ý",
                "Tôi có thể",
                "Tôi viết",
                "Tôi đã",
                "Lời chúc:",
                "Tôi hy vọng",
            };

            // Thử lấy phần sau dấu ':' khi trước đó là cụm giải thích ngắn
            int colon = response.IndexOf(':');
            if (colon > 0)
            {
                var before = response.Substring(0, colon).Trim();
                foreach (var p in skipLinePrefixes)
                {
                    if (before.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    {
                        var after = response.Substring(colon + 1).Trim();
                        after = after.Trim('"', '“', '”', '\'', '’', '«', '»');
                        if (!string.IsNullOrWhiteSpace(after))
                            return after;
                    }
                }
            }

            // 3) Rà từng dòng, tìm đoạn (paragraph) đầu tiên không phải giải thích
            var lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
            var cleaned = new List<string>();
            bool startedParagraph = false;
            foreach (var raw in lines)
            {
                var line = (raw ?? string.Empty).Trim();
                if (!startedParagraph)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue; // bỏ khoảng trắng đầu

                    bool skip = false;
                    foreach (var p in skipLinePrefixes)
                    {
                        if (line.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                        continue;

                    // Bắt đầu lấy đoạn này làm lời chúc
                    startedParagraph = true;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    // Kết thúc đoạn đầu
                    break;
                }

                if (line.StartsWith("- ")) line = line.Substring(2).Trim();
                line = line.Trim('"', '“', '”', '\'', '’', '«', '»');
                cleaned.Add(line);
            }

            var result = string.Join(" ", cleaned);
            result = result.Trim('"', '“', '”', '\'', '’', '«', '»').Trim();
            return string.IsNullOrWhiteSpace(result) ? response.Trim() : result;
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
