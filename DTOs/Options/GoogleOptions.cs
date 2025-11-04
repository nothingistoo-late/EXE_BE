namespace DTOs.Options
{
    public class GoogleOptions
    {
        public const string SectionName = "Authentication:Google";
        
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }
}

