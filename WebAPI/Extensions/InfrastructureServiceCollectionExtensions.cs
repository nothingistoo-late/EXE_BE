using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Repositories.Implementations;
using Repositories.Implements;
using Repositories.Interfaces;
using Services.Helpers.Mappers;
using System.Text;
using Services.Commons.Gmail;
using Services.Options;
using Services.Interfaces;
using Services.Implementations;
using System.Security.Claims;


namespace WebAPI.Extensions
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Cấu hình Settings
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<HuggingFaceOptions>(configuration.GetSection("HuggingFace"));
            services.Configure<GroqOptions>(configuration.GetSection("Groq"));

            // 2. DbContext và CORS
            services.AddDbContext<EXE_BE>(opt =>
                opt.UseSqlServer(
                    configuration.GetConnectionString("EXE_BE"),
                    sql => sql.MigrationsAssembly("Repositories")));
            services.AddCors(opt =>
            {
                opt.AddPolicy("CorsPolicy", b => b
                    .WithOrigins("https://www.vegie.site", "https://vegie.site", "http://localhost:5173")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetIsOriginAllowedToAllowWildcardSubdomains());
            });

            // 3. Identity & Authentication
            services.AddIdentity<User, Role>(opts =>
            {
                // Bắt buộc phải xác thực email mới cho SignIn
                opts.SignIn.RequireConfirmedEmail = true;
                opts.ClaimsIdentity.UserNameClaimType = ClaimTypes.Name;
                opts.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
                opts.Lockout.MaxFailedAccessAttempts = 5;
                opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                opts.Lockout.AllowedForNewUsers = true;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequireDigit = false;
                opts.Password.RequireLowercase = false;
                opts.Password.RequireUppercase = false;
                opts.Password.RequiredLength = 4;
            })
            .AddEntityFrameworkStores<EXE_BE>()
            .AddDefaultTokenProviders();

            var jwt = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                      ?? throw new InvalidOperationException("JWT key is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.ValidIssuer,
                    ValidAudience = jwt.ValidAudience,
                    IssuerSigningKey = key
                };
                // Custom error handling
                opts.Events = new JwtBearerEvents
                {
                    OnChallenge = ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "application/json";
                        var res = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = "You are not authorized. Please authenticate."
                        });
                        return ctx.Response.WriteAsync(res);
                    }
                };
            });

            // 4. Repositories & Domain Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRepositoryFactory, RepositoryFactory>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ICurrentTime, CurrentTime>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IBoxTypeRepository, BoxTypeRepository>();
            services.AddScoped<IDiscountRepository, DiscountRepository>();
            services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
            services.AddScoped<ISubscriptionPackageRepository, SubscriptionPackageRepository>();
            services.AddScoped<ICustomerSubscriptionRepository, CustomerSubscriptionRepository>();
            services.AddScoped<IHealthSurveyRepository, HealthSurveyRepository>();
            services.AddScoped<IAiRecipeRepository, AiRecipeRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IUserDiscountRepository, UserDiscountRepository>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserEmailService, UserEmailService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEXEGmailService, EXEGmailService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            // Use GroqAIService for AI functionality
            services.AddScoped<IAIService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var groqOptions = config.GetSection("Groq").Get<GroqOptions>();
                var httpClient = provider.GetRequiredService<HttpClient>();
                return new GroqAIService(httpClient, groqOptions?.ApiKey ?? string.Empty, groqOptions?.Model ?? "llama-3.1-8b-instant");
            });

            // OTP Service
            services.AddScoped<IOTPService, OTPService>();

        // Email Automation Service
        services.AddScoped<IEmailAutomationService, EmailAutomationService>();
        
        // Payment Failure Tracking Service
        services.AddScoped<IPaymentFailureTrackingService, PaymentFailureTrackingService>();
        
        // Pending Order Tracking Service
        services.AddScoped<IPendingOrderTrackingService, PendingOrderTrackingService>();
            // services.AddScoped<IHuggingFaceService, MockAIService>();
            // services.AddScoped<IHuggingFaceService, HuggingFaceService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IDiscountService, DiscountService>();
            services.AddScoped<IBoxTypeService, BoxTypeService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<ICustomerSubscriptionService, CustomerSubscriptionService>();
            services.AddScoped<ISubscriptionPackageService, SubscriptionPackageService>();
               services.AddScoped<IImageSearchService, ImageSearchService>();
               services.AddScoped<IAiMenuService, AiMenuService>();
               services.AddScoped<IGiftBoxService, GiftBoxService>();
               services.AddScoped<INotificationService, NotificationService>();
            //services.AddScoped<IHealthSurveyService, HealthSurveyService>();
            services.AddHttpClient<ChatBoxAI.Services.IGeminiService, ChatBoxAI.Services.GeminiService>();
            services.Configure<ChatBoxAI.Options.GeminiOptions>(
                configuration.GetSection(ChatBoxAI.Options.GeminiOptions.SectionName));
            
            // 5. Email + Quartz
            services.AddEmailServices(options =>
            {
                configuration.GetSection("EmailSettings").Bind(options);
                //options.SchoolName = "Trường Tiểu học Lê Văn Việt";
            });

            // 6. Controllers
            services.AddControllers();

            // 7. Mapper
            services.AddAutoMapper(typeof(MappingProfile).Assembly);

            return services;
        }
    }
}
