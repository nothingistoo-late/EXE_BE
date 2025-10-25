using Microsoft.AspNetCore.Mvc;
using Services.Commons.Gmail;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailTestController> _logger;

        public EmailTestController(IEmailService emailService, ILogger<EmailTestController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Test gửi email đơn giản
        /// </summary>
        [HttpPost("send-simple")]
        public async Task<IActionResult> SendSimpleEmail([FromBody] SimpleEmailRequest request)
        {
            try
            {
                _logger.LogInformation("Testing simple email send to: {Email}", request.ToEmail);
                
                await _emailService.SendEmailAsync(request.ToEmail, request.Subject, request.Message);
                
                return Ok(new { 
                    Success = true, 
                    Message = "Email sent successfully",
                    To = request.ToEmail,
                    Subject = request.Subject
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send simple email to: {Email}", request.ToEmail);
                return BadRequest(new { 
                    Success = false, 
                    Message = ex.Message,
                    Error = ex.ToString()
                });
            }
        }

        /// <summary>
        /// Test gửi email HTML
        /// </summary>
        [HttpPost("send-html")]
        public async Task<IActionResult> SendHtmlEmail([FromBody] HtmlEmailRequest request)
        {
            try
            {
                _logger.LogInformation("Testing HTML email send to: {Email}", request.ToEmail);
                
                var htmlMessage = $@"
                    <html>
                    <body>
                        <h2 style='color: #2E8B57;'>{request.Title}</h2>
                        <p>{request.Content}</p>
                        <hr>
                        <p style='color: #666; font-size: 12px;'>
                            This is a test email from EXE Food Delivery System
                        </p>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(request.ToEmail, request.Subject, htmlMessage);
                
                return Ok(new { 
                    Success = true, 
                    Message = "HTML email sent successfully",
                    To = request.ToEmail,
                    Subject = request.Subject,
                    Title = request.Title
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send HTML email to: {Email}", request.ToEmail);
                return BadRequest(new { 
                    Success = false, 
                    Message = ex.Message,
                    Error = ex.ToString()
                });
            }
        }

        /// <summary>
        /// Test gửi email cho nhiều người
        /// </summary>
        [HttpPost("send-bulk")]
        public async Task<IActionResult> SendBulkEmail([FromBody] BulkEmailRequest request)
        {
            try
            {
                _logger.LogInformation("Testing bulk email send to {Count} recipients", request.ToEmails.Count);
                
                await _emailService.SendEmailAsync(request.ToEmails, request.Subject, request.Message);
                
                return Ok(new { 
                    Success = true, 
                    Message = "Bulk email sent successfully",
                    Recipients = request.ToEmails,
                    Subject = request.Subject,
                    Count = request.ToEmails.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk email to {Count} recipients", request.ToEmails.Count);
                return BadRequest(new { 
                    Success = false, 
                    Message = ex.Message,
                    Error = ex.ToString()
                });
            }
        }

        /// <summary>
        /// Test email settings configuration
        /// </summary>
        [HttpGet("test-config")]
        public IActionResult TestEmailConfig()
        {
            try
            {
                return Ok(new { 
                    Message = "Email configuration test endpoint",
                    Timestamp = DateTime.UtcNow.AddHours(7),
                    Status = "Ready to send emails"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test email configuration");
                return BadRequest(new { 
                    Success = false, 
                    Message = ex.Message
                });
            }
        }
    }

    public class SimpleEmailRequest
    {
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class HtmlEmailRequest
    {
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class BulkEmailRequest
    {
        public List<string> ToEmails { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
