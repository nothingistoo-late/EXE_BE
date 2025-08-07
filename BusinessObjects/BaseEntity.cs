namespace BusinessObjects
{
    public abstract class BaseEntity
    {
        public string? Note { get; set; } 
        public string? ImgURL { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; } 
        public Guid? DeletedBy { get; set; }
    }
}
