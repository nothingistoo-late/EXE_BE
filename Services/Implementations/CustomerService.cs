using AutoMapper;
using DTOs;
using DTOs.Customer.Request;
using DTOs.Customer.Responds;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Repositories.Implements;
using Repositories.Interfaces;
using Services.Commons;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Commons.Gmail;

namespace Services.Implementations
{
    public class CustomerService : BaseService<Customer, Guid>, ICustomerService
    {


        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly EXE_BE _context;
        private readonly IEXEGmailService _emailService;
        
        public CustomerService(EXE_BE context, IMapper mapper, UserManager<User> usermanager,IGenericRepository<Customer, Guid> repository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ICurrentTime currentTime, IEXEGmailService emailService) : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _userManager = usermanager;
            _mapper = mapper;   
            _context = context;
            _emailService = emailService;
        }

        public async Task<ApiResult<CreateCustomerRequestDTO>> CreateCustomerAsync(CreateCustomerRequestDTO dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync(); // 🚀 Mở transaction

                // Bước 1: Tạo User
                var user = _mapper.Map<User>(dto);
                var existingUser = await _userManager.FindByEmailAsync(user.Email);
                if (existingUser != null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ApiResult<CreateCustomerRequestDTO>.Failure(new Exception("Email đã được sử dụng bởi người dùng khác."));
                }
                existingUser = await _unitOfWork.UserRepository.FirstOrDefaultAsync(c => c.PhoneNumber == dto.PhoneNumber);
                if (existingUser != null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ApiResult<CreateCustomerRequestDTO>.Failure(new Exception("Số điện thoại đã được sử dụng bởi người dùng khác."));
                }

                var createUserResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createUserResult.Succeeded)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ApiResult<CreateCustomerRequestDTO>.Failure(new Exception("Tạo user thất bại: " + string.Join(", ", createUserResult.Errors.Select(e => e.Description))));
                } else 
                    await _userManager.AddToRoleAsync(user, "USER"); // Thêm vào role Customer

                // Bước 2: Tạo Customer
                var customer = new Customer
                    {
                        UserId = user.Id,
                        Address = dto.Address,
                        CreatedAt = _currentTime.GetVietnamTime(),
                };
                user.CreatedAt = _currentTime.GetVietnamTime();

                await _repository.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();

                // ✅ Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                // Gửi email chào mừng cho khách hàng mới
                try
                {
                    await _emailService.SendRegistrationSuccessEmailAsync(user.Email, user.FullName);
                }
                catch (Exception emailEx)
                {
                    // Log lỗi email nhưng không làm fail transaction
                    // Có thể log vào file hoặc database
                }

                return ApiResult<CreateCustomerRequestDTO>.Success(dto,"Tạo User + Customer thành công!");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResult<CreateCustomerRequestDTO>.Failure(new Exception("Có lỗi xảy ra: " + ex.Message));
            }
        }

        public async Task<ApiResult<List<CustomerRespondDTO>>> GetAllCustomersAsync()
        {
            var customers = await _repository.GetAllAsync(
                            predicate: c => !c.IsDeleted,
                            includes: c => c.User);
            var result = _mapper.Map<List<CustomerRespondDTO>>(customers);
            return ApiResult<List<CustomerRespondDTO>>.Success(result,"Lấy thông tin thành công!!");
        }


        public async Task<ApiResult<CustomerRespondDTO>> GetCustomerByIdAsync(Guid Id)
        {
            try
            {
                var customer = await _repository.FirstOrDefaultAsync(c => c.UserId == Id && !c.IsDeleted,
                    includes : c => c.User
                );

                if (customer == null)
                {
                    return ApiResult<CustomerRespondDTO>.Failure(new Exception("Không tìm thấy khách hàng!"));
                }

                var resultDto = _mapper.Map<CustomerRespondDTO>(customer);
                return ApiResult<CustomerRespondDTO>.Success(resultDto,"Tìm thấy khách hàng thành công!!");
            }
            catch (Exception ex)
            {
                return ApiResult<CustomerRespondDTO>.Failure(new Exception("Lỗi khi lấy khách hàng! " + ex.Message));
            }
        }

        public async Task<ApiResult<MyProfileResponse?>> GetMyProfileAsync()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                if (userId == null)
                {
                    return ApiResult<MyProfileResponse?>.Failure(new Exception("Không tìm thấy thông tin người dùng hiện tại!!\n Hãy thử đăng nhập lại và thử lại sau!!"));
                }

                var customer = await _unitOfWork.CustomerRepository
                                   .GetQueryable()
                                   .Include(c => c.User)
                                   .Where(c => c.UserId == userId)
                                   .FirstOrDefaultAsync();
                if (customer == null || customer.IsDeleted)
                {
                    return ApiResult<MyProfileResponse?>.Failure(new Exception("Không tìm thấy thông tin cá nhân của bạn!"));
                }

                var resultDto = _mapper.Map<MyProfileResponse>(customer);
                return ApiResult<MyProfileResponse?>.Success(resultDto, "Lấy thông tin cá nhân thành công!");

            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy thông tin cá nhân: " + ex.Message);
            }
        }

        //public async Task<ApiResult<CustomerRespondDTO>> SoftDeleteCustomerById(Guid customerId)
        //{
        //    try
        //    {
        //        var customer = await _repository.GetByIdAsync(customerId, c => c.User);

        //        if (customer == null || customer.IsDeleted)
        //        {
        //            return ApiResult<CustomerRespondDTO>.Failure(new Exception("Không tìm thấy khách hàng!"));
        //        }

        //        customer.IsDeleted = true;
        //        customer.DeletedAt = _currentTime.GetVietnamTime();
        //        customer.DeletedBy = _currentUserService.GetUserId();

        //        await _repository.UpdateAsync(customer);
        //        await _unitOfWork.SaveChangesAsync();

        //        var resultDto = _mapper.Map<CustomerRespondDTO>(customer);
        //        return ApiResult<CustomerRespondDTO>.Success(resultDto, "Xóa mềm khách hàng thành công!");
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResult<CustomerRespondDTO>.Failure(new Exception("Lỗi khi xóa mềm khách hàng! " + ex.Message));
        //    }
        //}

        public async Task<ApiResult<CustomerRespondDTO>> SoftDeleteCustomerById(Guid customerId)
        {
            try
            {
                var customer = await _repository.FirstOrDefaultAsync(c => c.UserId  == customerId, c => c.User);
                if (customer == null || customer.IsDeleted)
                    return ApiResult<CustomerRespondDTO>.Failure(new Exception("Không tìm thấy khách hàng để xóa!"));

                var userId = customer.UserId;
                var currentUserId = _currentUserService.GetUserId();

                // Xóa mềm Customer
                var deletedCustomer = await _repository.SoftDeleteAsync(customerId, currentUserId);

                // Xóa mềm User (liên kết với customer)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null && !user.IsDeleted)
                {
                    user.IsDeleted = true;
                    user.UpdatedAt = _currentTime.GetVietnamTime();
                    user.UpdatedBy = currentUserId ?? Guid.Empty;

                    _context.Users.Update(user);
                }

                await _unitOfWork.SaveChangesAsync();

                var dto = _mapper.Map<CustomerRespondDTO>(customer);
                return ApiResult<CustomerRespondDTO>.Success(dto, "Xóa mềm khách hàng + tài khoản thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<CustomerRespondDTO>.Failure(new Exception("Lỗi khi xóa mềm khách hàng: " + ex.Message));
            }
        }

        public async Task<ApiResult<MyProfileResponse>> UpdateMyProfileAsync(UpdateMyProfileRequest request)
        {
            try
            {

                var userId = _currentUserService.GetUserId();
                if (userId == null)
                {
                    return ApiResult<MyProfileResponse>.Failure(new Exception("Không tìm thấy thông tin người dùng hiện tại!!\n Hãy thử đăng nhập lại và thử lại sau!!"));
                }

                var customer = await _unitOfWork.CustomerRepository
                    .GetQueryable()
                    .Include(c => c.User)
                    .Where(c => c.UserId == userId)
                    .FirstOrDefaultAsync();

                if (customer == null)
                    return ApiResult<MyProfileResponse>.Failure(new Exception("Customer not found"));

                var user = customer.User;

                // --- USER ---
                // Tách FullName thành FirstName và LastName
                if (!string.IsNullOrWhiteSpace(request.FullName))
                {
                    var nameParts = request.FullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length > 0)
                    {
                        user.FirstName = nameParts[0];
                        // Nếu có nhiều hơn 1 phần, gộp phần còn lại thành LastName
                        if (nameParts.Length > 1)
                        {
                            user.LastName = string.Join(" ", nameParts.Skip(1));
                        }
                        else
                        {
                            user.LastName = string.Empty;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                    user.PhoneNumber = request.PhoneNumber;

                if (request.Gender.HasValue)
                    user.Gender = request.Gender.Value; // ví dụ "Male"

                user.UpdatedAt = _currentTime.GetVietnamTime();
                user.UpdatedBy = userId.Value;

                // --- CUSTOMER ---
                if (!string.IsNullOrWhiteSpace(request.Address))
                    customer.Address = request.Address;

                if (!string.IsNullOrWhiteSpace(request.ImgURL))
                    customer.ImgURL = request.ImgURL;


                customer.UpdatedAt = _currentTime.GetVietnamTime();
                customer.UpdatedBy = userId.Value;

                await _unitOfWork.CustomerRepository.UpdateAsync(customer);
                await _unitOfWork.SaveChangesAsync();

                // Map kết quả trả về
                var result = _mapper.Map<MyProfileResponse>(customer);
                return ApiResult<MyProfileResponse>.Success(result,"Cập nhật thông tin cá nhân thành công!!");
            }
            catch (Exception ex)
            {
                return ApiResult<MyProfileResponse>.Failure(new Exception("Lỗi khi cập nhật thông tin cá nhân: " + ex.Message));
            }
        }

        public async Task<ApiResult<string>> ChangePasswordAsync(CustomerChangePasswordRequest request)
        {
            try
            {
                // 1. Validate request
                if (request == null)
                    return ApiResult<string>.Failure(new ArgumentException("Request không hợp lệ"));

                if (string.IsNullOrWhiteSpace(request.OldPassword))
                    return ApiResult<string>.Failure(new ArgumentException("Mật khẩu cũ không được để trống"));

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                    return ApiResult<string>.Failure(new ArgumentException("Mật khẩu mới không được để trống"));

                if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                    return ApiResult<string>.Failure(new ArgumentException("Xác nhận mật khẩu không được để trống"));

                // 2. Validate NewPassword == ConfirmPassword
                if (request.NewPassword != request.ConfirmPassword)
                    return ApiResult<string>.Failure(new ArgumentException("Mật khẩu mới và xác nhận mật khẩu không khớp"));

                // 3. Lấy userId từ token
                var userId = _currentUserService.GetUserId();
                if (userId == null)
                    return ApiResult<string>.Failure(new InvalidOperationException("Không tìm thấy thông tin người dùng hiện tại. Vui lòng đăng nhập lại."));

                // 4. Lấy user từ database
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return ApiResult<string>.Failure(new InvalidOperationException("Không tìm thấy người dùng"));

                // 5. Verify old password
                var isOldPasswordCorrect = await _userManager.CheckPasswordAsync(user, request.OldPassword);
                if (!isOldPasswordCorrect)
                    return ApiResult<string>.Failure(new UnauthorizedAccessException("Mật khẩu cũ không đúng"));

                // 6. Đổi password
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    var errors = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
                    return ApiResult<string>.Failure(new InvalidOperationException($"Không thể đổi mật khẩu: {errors}"));
                }

                // 7. Update security stamp (để invalidate các token cũ)
                await _userManager.UpdateSecurityStampAsync(user);

                // 8. Gửi email thông báo
                try
                {
                    var userName = user.UserName ?? user.Email ?? "Người dùng";
                    await _emailService.SendPasswordChangedEmailAsync(user.Email ?? string.Empty, userName);
                }
                catch (Exception emailEx)
                {
                    // Log lỗi email nhưng không làm fail việc đổi mật khẩu
                    // Có thể log vào file hoặc database nếu cần
                }

                return ApiResult<string>.Success("Đổi mật khẩu thành công", "Mật khẩu đã được thay đổi thành công. Email thông báo đã được gửi đến bạn.");
            }
            catch (Exception ex)
            {
                return ApiResult<string>.Failure(new Exception($"Lỗi khi đổi mật khẩu: {ex.Message}"));
            }
        }
    }
}
