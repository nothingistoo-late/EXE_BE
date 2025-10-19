using BusinessObjects;

namespace Repositories.Interfaces
{
    public interface IUserDiscountRepository : IGenericRepository<UserDiscount, Guid>
    {
        Task<bool> HasUserUsedDiscountAsync(Guid userId, Guid discountId);
        Task<UserDiscount?> GetUserDiscountAsync(Guid userId, Guid discountId);
    }
}

