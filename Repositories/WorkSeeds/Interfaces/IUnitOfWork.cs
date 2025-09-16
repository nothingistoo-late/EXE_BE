using Repositories.Interfaces;

namespace Repositories.WorkSeeds.Interfaces
{

    public interface IUnitOfWork : IGenericUnitOfWork
    {
        IUserRepository UserRepository { get; }
        ICustomerRepository CustomerRepository { get; }
        IBoxTypeRepository BoxTypeRepository { get; }
        IDiscountRepository DiscountRepository { get; }
        IOrderRepository OrderRepository { get; }

        IOrderDetailRepository OrderDetailRepository { get; }
    }
}
