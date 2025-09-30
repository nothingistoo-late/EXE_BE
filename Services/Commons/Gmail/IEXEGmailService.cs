using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Commons.Gmail
{
    public interface IEXEGmailService
    {
        Task SendRegistrationSuccessEmailAsync(string toEmail, string userName);
        Task SendOrderConfirmationEmailAsync(string toEmail, Order order);
        Task SendPaymentSuccessEmailAsync(string toEmail, Order order);
        Task SendNewOrderNotificationToAdminAsync(Order order);
        Task SendCustomEmailAsync(string toEmail, string subject, string body);

    }
}
