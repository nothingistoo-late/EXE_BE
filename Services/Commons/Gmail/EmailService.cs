using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Services.Commons.Gmail
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, HttpClient httpClient)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _httpClient = httpClient;
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://api.resend.com/");
            }
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            await SendEmailAsync(new List<string> { email }, subject, message);
        }

        public async Task SendEmailAsync(List<string> to, string subject, string message)
        {
            await Task.Delay(2000); // ~0.6s để đảm bảo không quá 2 request/s

            // Validate inputs
            if (to == null || to.Count == 0)
                throw new ArgumentException("Recipient list cannot be empty", nameof(to));
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject cannot be empty", nameof(subject));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));

            _logger.LogInformation("[Resend] Preparing to send email. Recipients: {Recipients}, SubjectLength: {SubjectLen}, HtmlLength: {HtmlLen}",
                string.Join(", ", to), subject.Length, message.Length);

            if (string.IsNullOrWhiteSpace(_emailSettings.ResendApiKey))
                throw new InvalidOperationException("ResendApiKey is not configured in EmailSettings");
            if (string.IsNullOrWhiteSpace(_emailSettings.FromEmail))
                throw new InvalidOperationException("FromEmail is not configured in EmailSettings");

            var payload = new
            {
                from = string.IsNullOrWhiteSpace(_emailSettings.FromName)
                    ? _emailSettings.FromEmail
                    : $"{_emailSettings.FromName} <{_emailSettings.FromEmail}>",
                to = to,
                subject = subject,
                html = message
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _emailSettings.ResendApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("[Resend] POST {Url} with payload size {Bytes} bytes", _httpClient.BaseAddress + "emails", Encoding.UTF8.GetByteCount(json));

            var response = await _httpClient.SendAsync(request);
            _logger.LogInformation("[Resend] Response status: {StatusCode}", (int)response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Resend API failed with status {StatusCode}: {Body}", (int)response.StatusCode, body);
                response.EnsureSuccessStatusCode();
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var id = doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                _logger.LogInformation("[Resend] Email sent successfully. Id: {Id}, To: {Recipients}", id ?? "(unknown)", string.Join(", ", to));
            }
            catch
            {
                _logger.LogInformation("[Resend] Email sent successfully. To: {Recipients}. Raw response: {Body}", string.Join(", ", to), responseBody);
            }
        }
    }
}