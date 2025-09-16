using AutoMapper;
using DTOs.CartDTOs.Request;
using DTOs.CartDTOs.Respond;
using Services.Commons;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.WorkSeeds.Extensions;

namespace Services.Implementations
{
    public class CartService : BaseService<Order, Guid>, ICartService
    {
        private readonly IMapper _mapper;

        public CartService(IMapper mapper, IGenericRepository<Order, Guid> repository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime) :
            base(repository,
                currentUserService,
                unitOfWork,
                currentTime)
        {
            _mapper = mapper;

        }

        // ====================== GET CART ======================
        public async Task<ApiResult<CartResponse>> GetCartAsync(Guid userId)
        {
            try
            {
                var customer = await _unitOfWork.CustomerRepository
                      .FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                    return ApiResult<CartResponse>.Failure(new Exception("Không tìm thấy khách hàng với ID: " + userId));

                var cart = await _unitOfWork.OrderRepository
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Cart,
                                            includes: o => o.OrderDetails);

                if (cart == null)
                    return ApiResult<CartResponse>.Failure(new Exception("Giỏ hàng của bạn đang trống!!"));

                var response = _mapper.Map<CartResponse>(cart);
                return ApiResult<CartResponse>.Success(response, "Lấy giỏ hàng thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<CartResponse>.Failure(new Exception("Có lỗi khi lấy giỏ hàng, nội dung lỗi: "+ex.Message));
            }
        }

        // ====================== ADD ITEM ======================
        public async Task<ApiResult<CartResponse>> AddItemAsync(Guid userId, AddItemDto dto)
        {
            try
            {
                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    // 1. Lấy customer
                    var customer = await _unitOfWork.CustomerRepository
                        .FirstOrDefaultAsync(c => c.UserId == userId);
                    if (customer == null)
                        return ApiResult<CartResponse>.Failure(
                            new Exception($"Không tìm thấy khách hàng với ID: {userId}"));

                    if (dto.Quantity <= 0)
                        return ApiResult<CartResponse>.Failure(
                            new Exception("Số lượng phải lớn hơn 0"));

                    // 2. Tìm giỏ hàng hiện có
                    var existingCart = await _unitOfWork.OrderRepository
                        .FirstOrDefaultAsync(
                            o => o.UserId == userId && o.Status == OrderStatus.Cart,
                            includes: o => o.OrderDetails);
                    var isNewCart = existingCart == null;
                    var cart = existingCart ?? new Order
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Status = OrderStatus.Cart,
                        TotalPrice = 0,
                        FinalPrice = 0,
                        OrderDetails = new List<OrderDetail>()
                    };

                    if (isNewCart)
                    {
                        await _unitOfWork.OrderRepository.AddAsync(cart);
                    }

                    // 3. Kiểm tra BoxType
                    var box = await _unitOfWork.BoxTypeRepository.GetByIdAsync(dto.BoxTypeId);
                    if (box == null || box.IsDeleted)
                        return ApiResult<CartResponse>.Failure(
                            new Exception("BoxType không tồn tại!"));

                    // 4. Add hoặc update item trong cart
                    var existingItem = cart.OrderDetails
                        .FirstOrDefault(d => d.BoxTypeId == dto.BoxTypeId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity += dto.Quantity;
                        existingItem.UnitPrice = box.Price;
                        // Không cần UpdateAsync vì entity đang được track
                    }
                    else
                    {
                        var detail = new OrderDetail
                        {
                            Id = Guid.NewGuid(),
                            OrderId = cart.Id,
                            BoxTypeId = dto.BoxTypeId,
                            Quantity = dto.Quantity,
                            UnitPrice = box.Price
                        };

                        await _unitOfWork.OrderDetailRepository.AddAsync(detail);
                    }

                    // 5. Tính lại giá
                    cart.TotalPrice = cart.OrderDetails.Sum(i => i.Quantity * i.UnitPrice)
                        + (existingItem == null ? dto.Quantity * box.Price : 0);
                    cart.FinalPrice = cart.TotalPrice;

                    // 6. SaveChanges một lần duy nhất
                    await _unitOfWork.SaveChangesAsync();

                    // 7. Reload cart to avoid duplicated navigation fixups
                    var freshCart = await _unitOfWork.OrderRepository
                        .GetByIdAsync(cart.Id, includes: o => o.OrderDetails);

                    return ApiResult<CartResponse>.Success(
                        _mapper.Map<CartResponse>(freshCart ?? cart),
                        "Thêm sản phẩm vào giỏ hàng thành công!");
                });
            }
            catch (Exception ex)
            {
                return ApiResult<CartResponse>.Failure(
                    new Exception("Có lỗi xảy ra khi thêm vào giỏ hàng, xin hãy thử lại sau!! " + ex.Message));
            }
        }


        // ====================== UPDATE QUANTITY ======================
        public async Task<ApiResult<CartResponse>> UpdateItemQuantityAsync(Guid userId, Guid orderDetailId, int quantity)
        {
            try
            {
                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var customer = await _unitOfWork.CustomerRepository
                       .FirstOrDefaultAsync(c => c.UserId == userId);
                    if (customer == null)
                        return ApiResult<CartResponse>.Failure(new Exception("Không tìm thấy khách hàng với ID: " + userId));

                    if (quantity < 0)
                        return ApiResult<CartResponse>.Failure(new Exception("Số lượng không hợp lệ"));

                    var cart = await _unitOfWork.OrderRepository
                        .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Cart,
                                                includes: o => o.OrderDetails);
                    if (cart == null)
                                return ApiResult<CartResponse>.Failure(new Exception("Giỏ hàng trống!!, Hãy thêm vật phẩm vào giỏ hàng trước đã nhé!!"));

                    var item = cart.OrderDetails.FirstOrDefault(x => x.Id == orderDetailId);
                    if (item == null)
                               return ApiResult<CartResponse>.Failure(new Exception("Sản phẩm với ID :"+orderDetailId+" không có trong giỏ!"));

                    if (quantity == 0)
                    {
                        await _unitOfWork.OrderDetailRepository.DeleteAsync(item.Id);
                        cart.OrderDetails.Remove(item);
                    }
                    else
                    {
                        item.Quantity = quantity;
                        await _unitOfWork.OrderDetailRepository.UpdateAsync(item);
                    }

                    cart.TotalPrice = cart.OrderDetails.Sum(i => i.Quantity * i.UnitPrice);
                    cart.FinalPrice = cart.TotalPrice;

                    await _unitOfWork.OrderRepository.UpdateAsync(cart);
                    await _unitOfWork.SaveChangesAsync();

                    return ApiResult<CartResponse>.Success(_mapper.Map<CartResponse>(cart),
                        "Cập nhật số lượng sản phẩm thành công!");
                });
            }
            catch (Exception ex)
            {
                return ApiResult<CartResponse>.Failure(new Exception("Có lỗi xảy ra khi cập nhật giỏ hàng!!!"+ ex.Message));
            }
        }

        // ====================== REMOVE ITEM ======================
        public async Task<ApiResult<bool>> RemoveItemAsync(Guid userId, Guid orderDetailId)
        {
            try
            {
                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var customer = await _unitOfWork.CustomerRepository
                        .FirstOrDefaultAsync(c => c.UserId == userId);
                    if (customer == null)
                        return ApiResult<bool>.Failure(new Exception("Không tìm thấy khách hàng với ID: "+userId));

                    var cart = await _unitOfWork.OrderRepository
                        .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Cart,
                                                includes: o => o.OrderDetails);
                    if (cart == null)
                        return ApiResult<bool>.Failure(new Exception("Giỏ hàng trống!!! Hãy thêm đồ vào giỏ hàng trước đã nhé!!"));

                    var item = cart.OrderDetails.FirstOrDefault(x => x.Id == orderDetailId);
                    if (item == null)
                               return ApiResult<bool>.Failure(new Exception("Sản phẩm không có trong giỏ!"));

                    await _unitOfWork.OrderDetailRepository.DeleteAsync(item.Id);
                    cart.OrderDetails.Remove(item);

                    cart.TotalPrice = cart.OrderDetails.Sum(i => i.Quantity * i.UnitPrice);
                    cart.FinalPrice = cart.TotalPrice;

                    await _unitOfWork.OrderRepository.UpdateAsync(cart);
                    await _unitOfWork.SaveChangesAsync();

                    return ApiResult<bool>.Success(true, "Xoá sản phẩm khỏi giỏ thành công!");
                });
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure(new Exception("Có lỗi xảy ra khi xóa sản phẩm khỏi giỏ hàng!!!"+ ex.Message));
            }
        }

        // ====================== CLEAR CART ======================
        public async Task<ApiResult<bool>> ClearCartAsync(Guid userId)
        {
            try
            {
                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var customer = await _unitOfWork.CustomerRepository
                      .FirstOrDefaultAsync(c => c.UserId == userId);
                    if (customer == null)
                        return ApiResult<bool>.Failure(new Exception("Không tìm thấy khách hàng với ID: " + userId));

                    var cart = await _unitOfWork.OrderRepository
                        .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Cart,
                                                includes: o => o.OrderDetails);

                    if (cart == null || !cart.OrderDetails.Any())
                        return ApiResult<bool>.Success(true, "Giỏ hàng đã trống!");

                    foreach (var d in cart.OrderDetails.ToList())
                    {
                        await _unitOfWork.OrderDetailRepository.DeleteAsync(d.Id);
                    }

                    cart.OrderDetails.Clear();
                    cart.TotalPrice = 0;
                    cart.FinalPrice = 0;

                    await _unitOfWork.OrderRepository.UpdateAsync(cart);
                    await _unitOfWork.SaveChangesAsync();

                    return ApiResult<bool>.Success(true, "Đã xoá toàn bộ giỏ hàng!");
                });
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure(new Exception("Có lỗi xảy ra khi xóa tất cả sản phẩm khỏi giỏ hàng!!!" + ex.Message));
            }
        }

        // ====================== CHECKOUT ======================
        public async Task<ApiResult<CartResponse>> CheckoutAsync(Guid userId, CheckoutDto dto)
        {
            try
            {
                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var customer = await _unitOfWork.CustomerRepository
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                    if (customer == null)
                        return ApiResult<CartResponse>.Failure(new Exception("Không tìm thấy khách hàng với ID: " + userId));

                    var cart = await _unitOfWork.OrderRepository
                        .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Cart,
                                                includes: o => o.OrderDetails);
                    if (cart == null)
                        return ApiResult<CartResponse>.Failure(new Exception("Giỏ hàng trống!!! Hãy thêm đồ vào giỏ hàng trước đã nhé!!"));

                    if (!cart.OrderDetails.Any())
                        return ApiResult<CartResponse>.Failure(new Exception("Giỏ hàng đang trống!"));

                    cart.PaymentMethod = dto.PaymentMethod;
                    cart.DeliveryMethod = dto.DeliveryMethod;
                    cart.DiscountCode = dto.DiscountCode;

                    // đảm bảo tính lại giá trước khi chốt
                    cart.TotalPrice = cart.OrderDetails.Sum(i => i.Quantity * i.UnitPrice);
                    cart.FinalPrice = cart.TotalPrice;

                    if (!string.IsNullOrWhiteSpace(dto.DiscountCode))
                    {
                        var currentTime = _currentTime.GetVietnamTime();

                        var discount = await _unitOfWork.DiscountRepository
                            .FirstOrDefaultAsync(d => d.Code == dto.DiscountCode && d.IsActive && d.EndDate>=currentTime);

                        if (discount == null)
                            return ApiResult<CartResponse>.Failure(new Exception("Mã giảm giá không tồn tại hoặc đã hết hạn!"));

                        cart.FinalPrice = discount.IsPercentage
                            ? cart.TotalPrice * (1 - discount.DiscountValue / 100)
                            : cart.TotalPrice - discount.DiscountValue;

                        if (cart.FinalPrice < 0)
                            cart.FinalPrice = 0;
                    }

                    cart.Status = OrderStatus.Pending;
                    cart.IsPaid = false;
                    cart.IsDelivered = false;

                    await _unitOfWork.OrderRepository.UpdateAsync(cart);
                    await _unitOfWork.SaveChangesAsync();

                    return ApiResult<CartResponse>.Success(_mapper.Map<CartResponse>(cart),
                        "Đặt hàng thành công!");
                });
            }
            catch (Exception ex)
            {
                return ApiResult<CartResponse>.Failure(new Exception("Có lỗi xảy ra khi đặt hàng!!!"+ ex.Message));
            }
        }
    }
}
