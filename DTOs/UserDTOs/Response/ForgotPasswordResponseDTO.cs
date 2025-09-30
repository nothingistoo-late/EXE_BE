namespace DTOs.UserDTOs.Response
{
    public class ForgotPasswordResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RemainingMinutes { get; set; }
    }
}
