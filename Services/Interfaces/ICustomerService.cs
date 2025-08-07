using DTOs;
using DTOs.Customer.Request;
using DTOs.Customer.Responds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ICustomerService
    {
        Task<ApiResult<CreateCustomerRequestDTO>> CreateCustomerAsync(CreateCustomerRequestDTO dto);
        Task<ApiResult<CustomerRespondDTO>> GetCustomerByIdAsync(Guid Id);
        Task<ApiResult<List<CustomerRespondDTO>>> GetAllCustomersAsync();
        //Task<ApiResult<CustomerRespondDTO>> UpdateCustomerById(UpdateCustomerRequestDTO request);
        Task<ApiResult<CustomerRespondDTO>> SoftDeleteCustomerById(Guid customerId);

        Task<ApiResult<MyProfileResponse?>> GetMyProfileAsync();
        Task<ApiResult<MyProfileResponse>> UpdateMyProfileAsync(UpdateMyProfileRequest request);
    }
}
