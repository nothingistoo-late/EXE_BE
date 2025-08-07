using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{
    public static class DBInitializer
    {
        public static async Task Initialize(
            EXE_BE context,
            UserManager<User> userManager)
        {
            #region Seed Roles
            if (!context.Roles.Any())
            {
                var roles = new List<Role>
                {
                    new Role { Name = "ADMIN", NormalizedName = "ADMIN" },
                    new Role { Name = "USER", NormalizedName = "USER" }
                };

                await context.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }
            #endregion

            #region Seed Users
            if (!context.Users.Any())
            {
                const string adminEmail = "trunghcse@gmail.com";
                if (await userManager.FindByEmailAsync(adminEmail) != null) return;

                var adminUser = CreateUser(adminEmail, "System", "Admin");
                const string adminPassword = "Admin@123";

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

            }
            #endregion

            // Cập nhật SecurityStamp cho các user nếu chưa có
            var allUsers = await context.Users.ToListAsync();
            foreach (var user in allUsers)
            {
                if (string.IsNullOrEmpty(user.SecurityStamp))
                {
                    await userManager.UpdateSecurityStampAsync(user);
                    Console.WriteLine($"Security stamp updated for user {user.UserName}");
                }
            }
        }

        private static async Task CreateUserAsync(UserManager<User> userManager, User user, string password, string role)
        {
            var userExist = await userManager.FindByEmailAsync(user.Email!);
            if (userExist == null)
            {
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
                else
                {
                    // Log lỗi chi tiết
                    var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Error creating user {user.Email}: {errorMsg}");
                }
            }
            else
            {
                Console.WriteLine($"User {user.Email} already exists.");
            }
        }
        private static User CreateUser(string email, string firstName, string lastName)
        {
            var now = DateTime.UtcNow;
            return new User
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                CreatedAt = now,
                CreatedBy = Guid.Empty,
                UpdatedAt = now,
                UpdatedBy = Guid.Empty,
                IsDeleted = false
            };
        }
    }
}
