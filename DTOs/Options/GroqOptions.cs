namespace Services.Options
{
    public class GroqOptions
    {
        public const string SectionName = "Groq";

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "llama-3.1-70b-versatile";
    }
}

