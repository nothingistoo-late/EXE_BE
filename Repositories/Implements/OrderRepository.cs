using Microsoft.EntityFrameworkCore;
using Repositories.WorkSeeds.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implements
{
    public class OrderRepository : GenericRepository<Order, Guid>, IOrderRepository
    {
        public OrderRepository(EXE_BE context) : base(context)
        {
        }
        public async Task<Order?> GetOrderWithDetailsAsync(Guid id)
        {
            return await _context.Set<Order>()
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.BoxType)
                .Include(o => o.Discount)
                .SingleOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetAllOrdersByCustomerIdAsync(Guid customerId)
        {
            return await _context.Set<Order>()
                .Where(o => o.UserId == customerId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.BoxType)
                .Include(o => o.Discount)
                .ToListAsync();
        }

    }
}
