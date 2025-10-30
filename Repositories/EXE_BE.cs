using BusinessObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{
    public class EXE_BE : IdentityDbContext<User,Role, Guid>
    {
        public EXE_BE(DbContextOptions<EXE_BE> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<BoxTypes> BoxTypes { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<CustomerSubscription> CustomerSubscriptions { get; set; }
        public DbSet<HealthSurvey> HealthSurveys { get; set; }
        public DbSet<SubscriptionPackage> SubscriptionPackages { get; set; }
        public DbSet<AiRecipe> AiRecipes { get; set; }
        public DbSet<GiftBoxOrder> GiftBoxOrders { get; set; }  
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserDiscount> UserDiscounts { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<WeeklyBlindBoxSubscription> WeeklyBlindBoxSubscriptions { get; set; }
        public DbSet<WeeklyDeliverySchedule> WeeklyDeliverySchedules { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

            // Configure AiRecipe entity
            modelBuilder.Entity<AiRecipe>(entity =>
            {
                entity.ToTable("AiRecipes");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DishName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Ingredients).IsRequired();
                entity.Property(e => e.Instructions).IsRequired();
                entity.Property(e => e.EstimatedCookingTime).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CookingTips).HasMaxLength(500);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
                entity.Property(e => e.InputVegetables).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.AiModel).IsRequired().HasMaxLength(50);
                entity.Property(e => e.GeneratedAt).IsRequired();
                
                // Configure relationship with User
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Notification entity
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.SenderId).IsRequired();
                entity.Property(e => e.ReceiverId).IsRequired(false);
                entity.Property(e => e.IsRead).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.CreatedBy).IsRequired();
                entity.Property(e => e.UpdatedBy).IsRequired();
                entity.Property(e => e.IsDeleted).IsRequired();
                
                // Configure relationship with Sender (User)
                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Configure relationship with Receiver (User)
                entity.HasOne(e => e.Receiver)
                    .WithMany()
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure UserDiscount entity
            modelBuilder.Entity<UserDiscount>(entity =>
            {
                entity.ToTable("UserDiscounts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.DiscountId).IsRequired();
                entity.Property(e => e.UsedAt).IsRequired();
                
                // Configure relationship with User
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserDiscounts)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Configure relationship with Discount
                entity.HasOne(e => e.Discount)
                    .WithMany(d => d.UserDiscounts)
                    .HasForeignKey(e => e.DiscountId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Create unique index to prevent duplicate usage
                entity.HasIndex(e => new { e.UserId, e.DiscountId })
                    .IsUnique()
                    .HasDatabaseName("IX_UserDiscounts_UserId_DiscountId");
            });

            // Configure Order entity
            // Note: Order chỉ được tạo khi user đăng ký gói để thanh toán
            // Lịch giao hàng được lưu riêng trong WeeklyDeliverySchedule, không link với orders

            // Configure WeeklyBlindBoxSubscription entity
            modelBuilder.Entity<WeeklyBlindBoxSubscription>(entity =>
            {
                entity.ToTable("WeeklyBlindBoxSubscriptions");
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.BoxType)
                    .WithMany()
                    .HasForeignKey(e => e.BoxTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.DeliverySchedules)
                    .WithOne(d => d.Subscription)
                    .HasForeignKey(d => d.SubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure WeeklyDeliverySchedule entity
            modelBuilder.Entity<WeeklyDeliverySchedule>(entity =>
            {
                entity.ToTable("WeeklyDeliverySchedules");
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Subscription)
                    .WithMany(s => s.DeliverySchedules)
                    .HasForeignKey(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Index để query nhanh theo subscription và tuần
                entity.HasIndex(e => new { e.SubscriptionId, e.WeekStartDate })
                    .IsUnique()
                    .HasDatabaseName("IX_WeeklyDeliverySchedules_SubscriptionId_WeekStartDate");
            });
        }
    }
}
