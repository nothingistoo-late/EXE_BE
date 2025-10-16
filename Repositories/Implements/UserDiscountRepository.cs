using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Implements;

namespace Repositories.Implements
{
    public class UserDiscountRepository : GenericRepository<UserDiscount, Guid>, IUserDiscountRepository
    {
        public UserDiscountRepository(EXE_BE context) : base(context)
        {
        }

        public async Task<bool> HasUserUsedDiscountAsync(Guid userId, Guid discountId)
        {
            return await _context.UserDiscounts
                .AnyAsync(ud => ud.UserId == userId && ud.DiscountId == discountId);
        }

        public async Task<UserDiscount?> GetUserDiscountAsync(Guid userId, Guid discountId)
        {
            return await _context.UserDiscounts
                .FirstOrDefaultAsync(ud => ud.UserId == userId && ud.DiscountId == discountId);
        }
    }
}
