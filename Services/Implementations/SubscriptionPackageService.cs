using Services.Commons;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class SubscriptionPackageService : BaseService<SubscriptionPackage, Guid>, ISubscriptionPackageService
    {
        public SubscriptionPackageService(IGenericRepository<SubscriptionPackage, Guid> repository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ICurrentTime currentTime) : base(repository, currentUserService, unitOfWork, currentTime)
        {
        }

        public async Task<ApiResult<List<SubscriptionPackage>>> GetAllAsync()
        {
            try
            {
                var packages = await _unitOfWork.SubscriptionPackageRepository.GetAllAsync();
                if (packages == null || !packages.Any())
                {
                    return ApiResult<List<SubscriptionPackage>>.Failure(new Exception("Không tìm thấy gói dịch vụ nào."));
                }
                return ApiResult<List<SubscriptionPackage>>.Success(packages.ToList(), "Lấy tất cả gói dịch vụ thành công!!");
            }
            catch (Exception ex)
            {
                return ApiResult<List<SubscriptionPackage>>.Failure(new Exception("Lỗi khi lấy tất cả các gói dịch vụ " + ex.Message));
            }
        }
    }
}
