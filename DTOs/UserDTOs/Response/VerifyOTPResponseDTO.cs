namespace DTOs.UserDTOs.Response
{
    public class VerifyOTPResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public int RemainingMinutes { get; set; }
    }
}
