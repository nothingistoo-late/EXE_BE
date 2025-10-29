namespace DTOs.Options
{
    public class CloudinaryOptions
    {
        public const string SectionName = "Cloudinary";
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string FolderPrefix { get; set; } = "uploads/avatars";
    }
}


