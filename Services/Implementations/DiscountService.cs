using AutoMapper;
using DTOs.DiscountDTOs.Request;
using DTOs.DiscountDTOs.Respond;
using Services.Commons;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class DiscountService : BaseService<Discount, Guid>, IDiscountService
    {
        private readonly IMapper _mapper;

        public DiscountService(IMapper mapper, IGenericRepository<Discount, Guid> repository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ICurrentTime currentTime) : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _mapper = mapper;

        }


        public async Task<ApiResult<DiscountRespondDTO>> GetDiscountByIdAsync(Guid id)
        {
            try
            {
                var discount = await _repository.GetByIdAsync(id);
                if (discount == null || !discount.IsActive || discount.EndDate < _currentTime.GetVietnamTime())
                {
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Không tìm thấy hoặc mã giảm giá đã hết hạn!"));
                }

                var dto = _mapper.Map<DiscountRespondDTO>(discount);
                return ApiResult<DiscountRespondDTO>.Success(dto, "Tìm thấy mã giảm giá thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<DiscountRespondDTO>.Failure(
                    new Exception("Lỗi khi lấy mã giảm giá: " + ex.Message));
            }
        }

        // ✅ Get all (chỉ active, chưa hết hạn)
        public async Task<ApiResult<List<DiscountRespondDTO>>> GetAllDiscountsAsync()
        {
            try
            {
                var discounts = await _repository.GetAllAsync();
                var active = discounts
                    .Where(x => x.IsActive && x.EndDate >= _currentTime.GetVietnamTime())
                    .ToList();

                var dtoList = _mapper.Map<List<DiscountRespondDTO>>(active);
                return ApiResult<List<DiscountRespondDTO>>.Success(dtoList, "Lấy danh sách mã giảm giá thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<List<DiscountRespondDTO>>.Failure(
                    new Exception("Lỗi khi lấy danh sách: " + ex.Message));
            }
        }

        // ✅ Create (validate ngày)
        public async Task<ApiResult<DiscountRespondDTO>> CreateDiscountAsync(DiscountCreateDTO dto)
        {
            try
            {
                if (dto.StartDate >= dto.EndDate)
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Ngày bắt đầu phải nhỏ hơn ngày kết thúc!"));
                var existing = (await _repository.GetAllAsync())
                    .FirstOrDefault(x => x.Code == dto.Code && x.IsActive);
                if (existing != null)
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Mã giảm giá đã tồn tại!"));
                if (dto.DiscountValue <= 0)
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Giá trị giảm giá phải lớn hơn 0!"));
                if (dto.IsPercentage && dto.DiscountValue > 100)
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Giá trị phần trăm giảm giá không được vượt quá 100%!"));
                var entity = _mapper.Map<Discount>(dto);
                entity.Id = Guid.NewGuid();
                entity.IsActive = true;

                await _repository.AddAsync(entity);
                await _unitOfWork.SaveChangesAsync();

                var result = _mapper.Map<DiscountRespondDTO>(entity);
                return ApiResult<DiscountRespondDTO>.Success(result, "Tạo mã giảm giá thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<DiscountRespondDTO>.Failure(
                    new Exception("Lỗi khi tạo mã giảm giá: " + ex.Message));
            }
        }

        // ✅ Update (patch + validate)
        public async Task<ApiResult<DiscountRespondDTO>> UpdateDiscountAsync(Guid id, DiscountUpdateDTO dto)
        {
            try
            {
                var discount = await _repository.GetByIdAsync(id);
                if (discount == null)
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Không tìm thấy mã giảm giá!"));

                // Patch từng field
                if (!string.IsNullOrEmpty(dto.Code)) discount.Code = dto.Code;
                if (!string.IsNullOrEmpty(dto.Description)) discount.Description = dto.Description;
                if (dto.DiscountValue.HasValue) discount.DiscountValue = dto.DiscountValue.Value;
                if (dto.IsPercentage.HasValue) discount.IsPercentage = dto.IsPercentage.Value;
                if (dto.StartDate.HasValue) discount.StartDate = dto.StartDate.Value;
                if (dto.EndDate.HasValue) discount.EndDate = dto.EndDate.Value;
                if (dto.IsActive.HasValue) discount.IsActive = dto.IsActive.Value;

                // Validate ngày nếu có chỉnh sửa
                if (discount.StartDate >= discount.EndDate)
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Ngày bắt đầu phải nhỏ hơn ngày kết thúc!"));

                await _repository.UpdateAsync(discount);
                await _unitOfWork.SaveChangesAsync();

                var result = _mapper.Map<DiscountRespondDTO>(discount);
                return ApiResult<DiscountRespondDTO>.Success(result, "Cập nhật mã giảm giá thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<DiscountRespondDTO>.Failure(
                    new Exception("Lỗi khi cập nhật mã giảm giá: " + ex.Message));
            }
        }

        // ✅ Soft Delete
        public async Task<ApiResult<bool>> DeleteDiscountAsync(Guid id)
        {
            try
            {
                var discount = await _repository.GetByIdAsync(id);
                if (discount == null)
                    return ApiResult<bool>.Failure(new Exception("Không tìm thấy mã giảm giá!"));

                discount.IsActive = false;
                await _repository.UpdateAsync(discount);
                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true, "Đã vô hiệu hoá mã giảm giá!");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure(new Exception("Lỗi khi xoá mã giảm giá: " + ex.Message));
            }
        }

        // ✅ Validate Discount Code - Kiểm tra mã giảm giá có hợp lệ không
        public async Task<ApiResult<DiscountRespondDTO>> ValidateDiscountCodeAsync(string code)
        {
            try
            {
                // Kiểm tra code có rỗng không
                if (string.IsNullOrWhiteSpace(code))
                {
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Mã giảm giá không được để trống!"));
                }

                // Lấy discount từ database
                var discount = await _unitOfWork.DiscountRepository.GetActiveDiscountByCodeAsync(code);
                
                // Không tìm thấy
                if (discount == null)
                {
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Mã giảm giá không tồn tại!"));
                }

                // Kiểm tra có bị xóa không
                if (discount.IsDeleted)
                {
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Mã giảm giá đã bị xóa!"));
                }

                // Kiểm tra có active không
                if (!discount.IsActive)
                {
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception("Mã giảm giá đã bị vô hiệu hóa!"));
                }

                var currentTime = _currentTime.GetVietnamTime();

                // Kiểm tra chưa đến ngày bắt đầu
                if (discount.StartDate > currentTime)
                {
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception($"Mã giảm giá chưa có hiệu lực! Thời gian bắt đầu: {discount.StartDate:dd/MM/yyyy HH:mm}"));
                }

                // Kiểm tra đã hết hạn chưa
                if (discount.EndDate < currentTime)
                {
                    return ApiResult<DiscountRespondDTO>.Failure(
                        new Exception($"Mã giảm giá đã hết hạn! Thời gian kết thúc: {discount.EndDate:dd/MM/yyyy HH:mm}"));
                }

                // Mã giảm giá hợp lệ
                var dto = _mapper.Map<DiscountRespondDTO>(discount);
                return ApiResult<DiscountRespondDTO>.Success(dto, 
                    $"Mã giảm giá hợp lệ! Giảm giá {discount.DiscountValue}{(discount.IsPercentage ? "%" : " VNĐ")}");
            }
            catch (Exception ex)
            {
                return ApiResult<DiscountRespondDTO>.Failure(
                    new Exception("Lỗi khi kiểm tra mã giảm giá: " + ex.Message));
            }
        }
    }
}
