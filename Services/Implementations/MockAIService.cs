using System;
using System.Threading;
using System.Threading.Tasks;
using Services.Interfaces;

namespace Services.Implementations
{
    public class MockAIService : IAIService
    {
        private readonly Random _random = new Random();

        public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
            }

            // Simulate API delay
            await Task.Delay(500, cancellationToken);

            // Generate mock response based on prompt
            var responses = new[]
            {
                $"Đây là phản hồi mock cho prompt: '{prompt}'. Tôi là AI mock service đang hoạt động tạm thời.",
                $"Mock AI đã xử lý yêu cầu của bạn: '{prompt}'. Đây là phản hồi được tạo tự động.",
                $"Xin chào! Tôi đã nhận được prompt '{prompt}' và đang trả lời bằng mock service.",
                $"Mock response: Tôi hiểu bạn muốn biết về '{prompt}'. Đây là phản hồi tạm thời từ mock AI.",
                $"Dựa trên prompt '{prompt}', tôi có thể cung cấp thông tin mock. Đây là dịch vụ tạm thời."
            };

            var randomResponse = responses[_random.Next(responses.Length)];
            return randomResponse;
        }

        public async Task<string> ChatAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));
            }

            // Simulate API delay
            await Task.Delay(800, cancellationToken);

            // Generate mock chat response based on message
            var chatResponses = new[]
            {
                $"Xin chào! Tôi đã nhận được tin nhắn: '{message}'. Đây là phản hồi từ mock chat service.",
                $"Cảm ơn bạn đã gửi tin nhắn: '{message}'. Tôi là AI mock đang hoạt động tạm thời.",
                $"Tôi hiểu bạn đang nói về: '{message}'. Đây là phản hồi mock từ chat service.",
                $"Mock chat response: Dựa trên tin nhắn '{message}', tôi có thể trò chuyện với bạn. Đây là dịch vụ tạm thời.",
                $"Tôi đã nhận được: '{message}'. Mock AI chat service đang hoạt động và sẵn sàng trò chuyện."
            };

            var randomChatResponse = chatResponses[_random.Next(chatResponses.Length)];
            return randomChatResponse;
        }
    }
}
