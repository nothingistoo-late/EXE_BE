namespace Services.Options
{
    public class HuggingFaceOptions
    {
        public const string SectionName = "HuggingFace";

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "facebook/blenderbot-400M-distill";
    }
}

