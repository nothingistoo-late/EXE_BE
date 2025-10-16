using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Repositories.Implements;
using System.Data;

namespace Repositories.WorkSeeds.Implements
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EXE_BE _context;
        private readonly IRepositoryFactory _repositoryFactory;
        private IDbContextTransaction? _transaction;
        private ILogger<UnitOfWork> _logger;
        // Specific repositories
        private IUserRepository? _userRepository;
        private ICustomerRepository? _customerRepository;
        private IBoxTypeRepository? _boxTypesRepository;
        private IDiscountRepository? _discountRepository;
        private IOrderRepository? _orderRepository;
        private IOrderDetailRepository? _orderDetailRepository;
        private ISubscriptionPackageRepository? _subscriptionPackageRepository;
        private ICustomerSubscriptionRepository? _customerSubscriptionRepository;
        private IHealthSurveyRepository? _healthSurveyRepository;
        private IUserDiscountRepository? _userDiscountRepository;
        public UnitOfWork(EXE_BE context, IRepositoryFactory repositoryFactory, ILogger<UnitOfWork> logger)
        {
            _context = context;
            _repositoryFactory = repositoryFactory;
            _logger = logger;
        }

        public IUserRepository UserRepository =>
            _userRepository ??= _repositoryFactory.GetCustomRepository<IUserRepository>();

        public ICustomerRepository CustomerRepository => 
            _customerRepository ??= _repositoryFactory.GetCustomRepository<ICustomerRepository>();
        public IBoxTypeRepository BoxTypeRepository => 
            _boxTypesRepository ??= _repositoryFactory.GetCustomRepository<IBoxTypeRepository>();
        public IDiscountRepository DiscountRepository => 
            _discountRepository ??= _repositoryFactory.GetCustomRepository<IDiscountRepository>();
        public IOrderRepository OrderRepository => 
            _orderRepository ??= _repositoryFactory.GetCustomRepository<IOrderRepository>();
        public IOrderDetailRepository OrderDetailRepository => 
            _orderDetailRepository ??= _repositoryFactory.GetCustomRepository<IOrderDetailRepository>();
        public ISubscriptionPackageRepository SubscriptionPackageRepository => 
            _subscriptionPackageRepository ??= _repositoryFactory.GetCustomRepository<ISubscriptionPackageRepository>();
        public ICustomerSubscriptionRepository CustomerSubscriptionRepository => 
            _customerSubscriptionRepository ??= _repositoryFactory.GetCustomRepository<ICustomerSubscriptionRepository>();
        public IHealthSurveyRepository HealthSurveyRepository => 
            _healthSurveyRepository ??= _repositoryFactory.GetCustomRepository<IHealthSurveyRepository>();
        public IUserDiscountRepository UserDiscountRepository => 
            _userDiscountRepository ??= _repositoryFactory.GetCustomRepository<IUserDiscountRepository>();
        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : class
        {
            return _repositoryFactory.GetRepository<TEntity, TKey>();
        }

        public bool HasActiveTransaction => _transaction != null;


        public async Task<IDbContextTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
                throw new InvalidOperationException("A transaction is already active.");

            _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return _transaction;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("No active transaction to commit.");

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                // ✨ FIXED: thay vì throw, log cảnh báo và return
                _logger.LogWarning("⚠️ No active transaction to rollback.");
                return;
            }

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
                _logger.LogInformation("🔁 Rollback transaction thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Rollback transaction gặp lỗi.");
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }


        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();

            if (_context != null)
                await _context.DisposeAsync();
        }
    }
}