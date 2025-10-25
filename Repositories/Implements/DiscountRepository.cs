using Microsoft.EntityFrameworkCore;
using Repositories.WorkSeeds.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implements
{
    public class DiscountRepository : GenericRepository<Discount, Guid>, IDiscountRepository
    {
        public DiscountRepository(EXE_BE context) : base(context)
        {
        }

        public async Task<Discount?> GetActiveDiscountByCodeAsync(string code)
        {
            var currentTime = DateTime.UtcNow.AddHours(7); // Vietnam time
            return await _context.Discounts
                .FirstOrDefaultAsync(d => d.Code == code && d.IsActive && !d.IsDeleted);
        }

        public async Task<List<Discount>> GetAllActiveDiscountsAsync()
        {
            return await _context.Discounts
                .Where(d => d.IsActive && !d.IsDeleted)
                .ToListAsync();
        }
    }
}
