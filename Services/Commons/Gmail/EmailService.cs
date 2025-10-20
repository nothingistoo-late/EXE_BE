using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Services.Commons.Gmail
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            await SendEmailAsync(new List<string> { email }, subject, message);
        }

        public async Task SendEmailAsync(List<string> to, string subject, string message)
        {
			// Email sending is disabled. This method returns without sending.
			_logger.LogInformation("Email sending is DISABLED. Skipping send to: {Recipients}", string.Join(", ", to ?? new List<string>()));
			
			/*
			// === ORIGINAL IMPLEMENTATION (COMMENTED OUT) ===
			_logger.LogInformation("=== STARTING EMAIL SEND PROCESS ===");
			_logger.LogInformation("Recipients: {Recipients}", string.Join(", ", to));
			_logger.LogInformation("Subject: {Subject}", subject);
			_logger.LogInformation("Message length: {MessageLength} characters", message?.Length ?? 0);
			
			if (to == null || to.Count == 0)
				throw new ArgumentException("Recipient list cannot be empty", nameof(to));
			if (string.IsNullOrEmpty(subject))
				throw new ArgumentException("Subject cannot be empty", nameof(subject));
			if (string.IsNullOrEmpty(message))
				throw new ArgumentException("Message cannot be empty", nameof(message));

			var email = new MimeMessage();
			
			// Kiểm tra email settings
			_logger.LogInformation("Checking EmailSettings configuration...");
			if (_emailSettings == null)
			{
				_logger.LogError("EmailSettings is NULL!");
				throw new InvalidOperationException("EmailSettings is not configured");
			}
			if (string.IsNullOrEmpty(_emailSettings.FromEmail))
			{
				_logger.LogError("FromEmail is NULL or empty!");
				throw new InvalidOperationException("FromEmail is not configured");
			}
			if (string.IsNullOrEmpty(_emailSettings.FromName))
			{
				_logger.LogError("FromName is NULL or empty!");
				throw new InvalidOperationException("FromName is not configured");
			}
				
			_logger.LogInformation("EmailSettings - SmtpServer: {SmtpServer}, SmtpPort: {SmtpPort}", 
				_emailSettings.SmtpServer, _emailSettings.SmtpPort);
			_logger.LogInformation("EmailSettings - SmtpUsername: {SmtpUsername}, FromEmail: {FromEmail}, FromName: {FromName}", 
				_emailSettings.SmtpUsername, _emailSettings.FromEmail, _emailSettings.FromName);
				
			email.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));

			foreach (var recipient in to)
			{
				_logger.LogInformation("Processing recipient: {Recipient}", recipient ?? "NULL");
				
				if (string.IsNullOrEmpty(recipient))
				{
					_logger.LogWarning("Skipping null or empty recipient email");
					continue;
				}
				
				try
				{
					email.To.Add(MailboxAddress.Parse(recipient));
					_logger.LogInformation("Successfully added recipient: {Recipient}", recipient);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to parse recipient: {Recipient}", recipient);
					throw;
				}
			}

			email.Subject = subject;
			email.Body = new TextPart("html") { Text = message };
			
			try
			{
				_logger.LogInformation("Creating SMTP client...");
				using var smtp = new SmtpClient();
				
				_logger.LogInformation("Connecting to SMTP server: {SmtpServer}:{SmtpPort}", 
					_emailSettings.SmtpServer, _emailSettings.SmtpPort);
				await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
				_logger.LogInformation("SMTP connection established successfully");
				
				_logger.LogInformation("Authenticating with SMTP server using username: {SmtpUsername}", 
					_emailSettings.SmtpUsername);
				await smtp.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
				_logger.LogInformation("SMTP authentication successful");
				
				_logger.LogInformation("Sending email to {Recipients}...", string.Join(", ", to));
				await smtp.SendAsync(email);
				_logger.LogInformation("Email sent successfully to {Recipients}", string.Join(", ", to));
				
				_logger.LogInformation("Disconnecting from SMTP server...");
				await smtp.DisconnectAsync(true);
				_logger.LogInformation("=== EMAIL SEND PROCESS COMPLETED SUCCESSFULLY ===");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "=== EMAIL SEND PROCESS FAILED ===");
				_logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", to));
				_logger.LogError(ex, "SMTP Server: {SmtpServer}:{SmtpPort}", _emailSettings.SmtpServer, _emailSettings.SmtpPort);
				_logger.LogError(ex, "SMTP Username: {SmtpUsername}", _emailSettings.SmtpUsername);
				throw;
			}
			*/

			return;
        }
    }
}