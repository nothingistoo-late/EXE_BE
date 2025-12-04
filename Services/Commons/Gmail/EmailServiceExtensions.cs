using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Services.Commons.Gmail
{
    public static class EmailServiceExtensions
    {
        public static IServiceCollection AddEmailServices(this IServiceCollection services, Action<EmailSettings> configureOptions)
        {
            services.Configure(configureOptions);
            services.AddSingleton<EmailQueue>();
            services.AddHttpClient<IEmailService, EmailService>();
            services.AddTransient<IEmailQueueService, EmailQueueService>();
            services.AddTransient<SendEmailJob>();
            services.AddHostedService<EmailBackgroundService>();
            // Disable daily reminder job to prevent automatic email sending
            // services.AddHostedService<EmailReminderService>();
            services.AddQuartz(q => { });
            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });

            return services;
        }
    }
}