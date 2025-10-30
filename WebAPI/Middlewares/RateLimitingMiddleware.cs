using BusinessObjects.Common;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace WebAPI.Middlewares
{
    public class RateLimitingMiddleware : IDisposable
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        
        // Lưu lịch sử request theo key (UserId/IP + Path)
        private static readonly ConcurrentDictionary<string, List<DateTime>> _requestHistory = new();
        // Lưu trạng thái block (khoá) sau khi vượt ngưỡng
        private static readonly ConcurrentDictionary<string, DateTime> _blockedUntil = new();
        // Timer dọn dẹp định kỳ
        private readonly Timer _cleanupTimer;
        
        private const int MAX_REQUESTS = 5; // Số lượng requests tối đa
        private const int TIME_WINDOW_SECONDS = 1; // Time window (1 giây)
        private const int BLOCK_SECONDS = 5; // Khoá trong 15 giây khi vượt ngưỡng
        
        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            // Bắt đầu dọn dẹp mỗi phút
            _cleanupTimer = new Timer(CleanupOldEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Key gồm danh tính client + path để hạn chế theo endpoint (ví dụ login)
            var key = GetClientIdentifier(context) + "|" + context.Request.Path.Value;
            
            // Nếu đang bị block, trả 429 + Retry-After
            if (IsBlocked(key, out var secondsLeft))
            {
                _logger.LogWarning("Blocked due to rate limit. Key: {Key}, seconds left: {Seconds}", key, secondsLeft);
                await HandleRateLimitExceeded(context, secondsLeft);
                return;
            }

            // Kiểm tra rate limit trong cửa sổ thời gian
            if (!IsRequestAllowed(key))
            {
                _logger.LogWarning("Rate limit exceeded for key: {Key}. Applying {Block}s block.", key, BLOCK_SECONDS);
                // Đặt block 15s
                _blockedUntil[key] = DateTime.UtcNow.AddSeconds(BLOCK_SECONDS);
                await HandleRateLimitExceeded(context, BLOCK_SECONDS);
                return;
            }
            
            // Record request
            RecordRequest(key);
            
            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Ưu tiên dùng UserId nếu đã authenticated
            var userIdClaim = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                return $"user:{userIdClaim}";
            }
            
            // Nếu chưa authenticated, dùng IP address
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip:{ipAddress}";
        }

        private bool IsBlocked(string key, out int secondsLeft)
        {
            secondsLeft = 0;
            if (_blockedUntil.TryGetValue(key, out var until))
            {
                var now = DateTime.UtcNow;
                if (until > now)
                {
                    secondsLeft = (int)Math.Ceiling((until - now).TotalSeconds);
                    return true;
                }
                else
                {
                    // Hết hạn block, xoá entry
                    _blockedUntil.TryRemove(key, out _);
                }
            }
            return false;
        }

        private bool IsRequestAllowed(string key)
        {
            var now = DateTime.UtcNow;
            var timeWindowStart = now.AddSeconds(-TIME_WINDOW_SECONDS);
            
            // Lấy hoặc tạo list requests cho key này
            var requests = _requestHistory.GetOrAdd(key, _ => new List<DateTime>());
            
            lock (requests)
            {
                // Xóa các requests cũ hơn time window
                requests.RemoveAll(r => r < timeWindowStart);
                
                // Kiểm tra số lượng requests trong time window
                return requests.Count < MAX_REQUESTS;
            }
        }

        private void RecordRequest(string key)
        {
            var requests = _requestHistory.GetOrAdd(key, _ => new List<DateTime>());
            var now = DateTime.UtcNow;
            
            lock (requests)
            {
                requests.Add(now);
            }
        }

        private static void CleanupOldEntries(object? state)
        {
            var cutoffTime = DateTime.UtcNow.AddSeconds(-TIME_WINDOW_SECONDS);
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _requestHistory)
            {
                lock (kvp.Value)
                {
                    kvp.Value.RemoveAll(r => r < cutoffTime);
                    
                    // Nếu list rỗng, đánh dấu để xóa key
                    if (kvp.Value.Count == 0)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }
            
            // Xóa các keys không còn requests
            foreach (var key in keysToRemove)
            {
                _requestHistory.TryRemove(key, out _);
            }

            // Cleanup các block đã hết hạn
            foreach (var kvp in _blockedUntil)
            {
                if (kvp.Value <= DateTime.UtcNow)
                {
                    _blockedUntil.TryRemove(kvp.Key, out _);
                }
            }
        }

        private static async Task HandleRateLimitExceeded(HttpContext context, int secondsLeft)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = secondsLeft.ToString();
            
            var response = new ApiResult<object>
            {
                IsSuccess = false,
                Message = $"Quá nhiều requests. Vui lòng thử lại sau {secondsLeft}s.",
                Data = null
            };
            
            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await context.Response.WriteAsync(jsonResponse);
        }

        public void Dispose()
        {
            _cleanupTimer.Dispose();
        }
    }

    // Extension method for easy registration
    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}

