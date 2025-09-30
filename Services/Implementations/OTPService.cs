using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Services.Interfaces;
using Repositories.Interfaces;

namespace Services.Implementations
{
    public class OTPService : IOTPService
    {
        private static readonly ConcurrentDictionary<string, OTPData> _otpStorage = new ConcurrentDictionary<string, OTPData>();
        private readonly Random _random;
        private readonly ICurrentTime _currentTime;
        private const int OTP_LENGTH = 6;
        private const int OTP_EXPIRY_MINUTES = 10;

        public OTPService(ICurrentTime currentTime)
        {
            _random = new Random();
            _currentTime = currentTime;
        }

        public async Task<string> GenerateOTPAsync(string email, string purpose = "ForgotPassword")
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            var key = GenerateKey(email, purpose);
            var otpCode = GenerateOTPCode();
            var now = _currentTime.GetVietnamTime();
            var expiryTime = now.AddMinutes(OTP_EXPIRY_MINUTES);

            var otpData = new OTPData
            {
                Code = otpCode,
                Email = email,
                Purpose = purpose,
                CreatedAt = now,
                ExpiresAt = expiryTime,
                IsUsed = false
            };

            _otpStorage.AddOrUpdate(key, otpData, (k, v) => otpData);
            
            // Debug logging
            Console.WriteLine($"DEBUG: Generated OTP for key: {key}, code: {otpCode}");
            Console.WriteLine($"DEBUG: Available keys after generation: {string.Join(", ", _otpStorage.Keys)}");
            Console.WriteLine($"DEBUG: OTP expires at: {expiryTime}");

            // Don't cleanup immediately after generation
            // await CleanupExpiredOTPsAsync();

            return otpCode;
        }

        public async Task<bool> VerifyOTPAsync(string email, string otpCode, string purpose = "ForgotPassword")
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
                return false;

            var key = GenerateKey(email, purpose);

            if (!_otpStorage.TryGetValue(key, out var otpData))
                return false;

            // Check if OTP is expired
            if (_currentTime.GetVietnamTime() > otpData.ExpiresAt)
            {
                _otpStorage.TryRemove(key, out _);
                return false;
            }

            // Check if OTP is already used
            if (otpData.IsUsed)
                return false;

            // Verify OTP code
            if (otpData.Code != otpCode)
                return false;

            // Don't mark as used here - let the caller decide when to mark as used
            return true;
        }

        public async Task<bool> VerifyAndMarkOTPAsUsedAsync(string email, string otpCode, string purpose = "ForgotPassword")
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
                return false;

            var key = GenerateKey(email, purpose);
            
            // Debug logging
            Console.WriteLine($"DEBUG: Verifying OTP for key: {key}");
            Console.WriteLine($"DEBUG: Available keys in storage: {string.Join(", ", _otpStorage.Keys)}");

            if (!_otpStorage.TryGetValue(key, out var otpData))
            {
                Console.WriteLine($"DEBUG: OTP not found for key: {key}");
                return false;
            }

            // Check if OTP is expired
            if (_currentTime.GetVietnamTime() > otpData.ExpiresAt)
            {
                _otpStorage.TryRemove(key, out _);
                return false;
            }

            // Check if OTP is already used
            if (otpData.IsUsed)
                return false;

            // Verify OTP code
            if (otpData.Code != otpCode)
                return false;

            // Mark as used
            otpData.IsUsed = true;
            _otpStorage.AddOrUpdate(key, otpData, (k, v) => otpData);

            return true;
        }

        public async Task RemoveOTPAsync(string email, string purpose = "ForgotPassword")
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            var key = GenerateKey(email, purpose);
            _otpStorage.TryRemove(key, out _);
        }

        public async Task<bool> IsOTPValidAsync(string email, string purpose = "ForgotPassword")
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var key = GenerateKey(email, purpose);

            if (!_otpStorage.TryGetValue(key, out var otpData))
                return false;

            // Check if OTP is expired
            if (_currentTime.GetVietnamTime() > otpData.ExpiresAt)
            {
                _otpStorage.TryRemove(key, out _);
                return false;
            }

            // Check if OTP is already used
            if (otpData.IsUsed)
                return false;

            return true;
        }

        public async Task<int> GetOTPRemainingMinutesAsync(string email, string purpose = "ForgotPassword")
        {
            if (string.IsNullOrWhiteSpace(email))
                return -1;

            var key = GenerateKey(email, purpose);

            if (!_otpStorage.TryGetValue(key, out var otpData))
                return -1;

            // Check if OTP is expired
            if (_currentTime.GetVietnamTime() > otpData.ExpiresAt)
            {
                _otpStorage.TryRemove(key, out _);
                return -1;
            }

            // Check if OTP is already used
            if (otpData.IsUsed)
                return -1;

            var remainingTime = otpData.ExpiresAt - _currentTime.GetVietnamTime();
            return (int)Math.Ceiling(remainingTime.TotalMinutes);
        }

        private string GenerateOTPCode()
        {
            return _random.Next(100000, 999999).ToString();
        }

        private string GenerateKey(string email, string purpose)
        {
            return $"{email.ToLowerInvariant()}_{purpose}";
        }

        private async Task CleanupExpiredOTPsAsync()
        {
            var expiredKeys = new List<string>();
            var now = _currentTime.GetVietnamTime();

            foreach (var kvp in _otpStorage)
            {
                if (now > kvp.Value.ExpiresAt)
                {
                    expiredKeys.Add(kvp.Key);
                    Console.WriteLine($"DEBUG: Removing expired OTP: {kvp.Key}, expired at: {kvp.Value.ExpiresAt}");
                }
            }

            foreach (var key in expiredKeys)
            {
                _otpStorage.TryRemove(key, out _);
            }
        }

        private class OTPData
        {
            public string Code { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Purpose { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsUsed { get; set; }
        }
    }
}
