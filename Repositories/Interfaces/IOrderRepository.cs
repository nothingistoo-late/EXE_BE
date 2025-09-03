using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order, Guid>
    {
        Task<Order?> GetOrderWithDetailsAsync(Guid id);
        Task<List<Order>> GetAllOrdersByCustomerIdAsync(Guid customerId);

    }
}
