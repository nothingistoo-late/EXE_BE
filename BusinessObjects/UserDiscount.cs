using BusinessObjects.Common;

namespace BusinessObjects
{
    public class UserDiscount : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid DiscountId { get; set; }
        public DateTime UsedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Discount Discount { get; set; } = null!;
    }
}