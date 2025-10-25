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
using Services.Commons.Gmail;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class CartService : BaseService<Order, Guid>, ICartService
    {
        private readonly IMapper _mapper;
        private readonly IEXEGmailService _emailService;
        private readonly ILogger<CartService> _logger;

        public CartService(IMapper mapper, IGenericRepository<Order, Guid> repository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            IEXEGmailService emailService,
            ILogger<CartService> logger) :
            base(repository,
                currentUserService,
                unitOfWork,
                currentTime)
        {
            _mapper = mapper;
            _emailService = emailService;
            _logger = logger;
        }

        // ====================== GET CART ======================
        public async Task<ApiResult<CartResponse>> GetCartAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("🛒 Getting cart for user {UserId}", userId);
                
                var customer = await _unitOfWork.CustomerRepository
                      .FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                {
                    _logger.LogWarning("❌ Customer not found for user {UserId}", userId);
                    return ApiResult<CartResponse>.Failure(new Exception("Không tìm thấy khách hàng với ID: " + userId));
                }

                var cart = await _unitOfWork.OrderRepository
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Cart,
                                            includes: o => o.OrderDetails);

                if (cart == null)
                {
                    _logger.LogInformation("🛒 No cart found for user {UserId}", userId);
                    return ApiResult<CartResponse>.Failure(new Exception("Giỏ hàng của bạn đang trống!!"));
                }

                _logger.LogInformation("✅ Cart retrieved for user {UserId} with {ItemCount} items", 
                    userId, cart.OrderDetails?.Count ?? 0);
                var response = _mapper.Map<CartResponse>(cart);
                return ApiResult<CartResponse>.Success(response, "Lấy giỏ hàng thành công!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get cart for user {UserId}", userId);
                return ApiResult<CartResponse>.Failure(new Exception("Có lỗi khi lấy giỏ hàng, nội dung lỗi: "+ex.Message));
            }
        }

        // ====================== GET CART WITH GIFTBOX INFO ======================
        public async Task<ApiResult<CartResponseWithGiftBox>> GetCartWithGiftBoxAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("🛒 Getting cart with GiftBox info for user {UserId}", userId);
                
                var customer = await _unitOfWork.CustomerRepository
                      .FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                {
                    _logger.LogWarning("❌ Customer not found for user {UserId}", userId);
                    return ApiResult<CartResponseWithGiftBox>.Failure(new Exception("Không tìm thấy khách hàng với ID: " + userId));
                }

                var cart = await _unitOfWork.OrderRepository
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Cart,
                                            includes: o => o.OrderDetails);

                if (cart == null)
                {
                    _logger.LogInformation("🛒 No cart found for user {UserId}", userId);
                    return ApiResult<CartResponseWithGiftBox>.Failure(new Exception("Giỏ hàng của bạn đang trống!!"));
                }

                // Get BoxType information for all items
                var boxTypeIds = cart.OrderDetails.Select(od => od.BoxTypeId).Distinct().ToList();
                var boxTypes = await _unitOfWork.BoxTypeRepository.GetByIdsAsync(boxTypeIds);
                var boxTypeMap = boxTypes.ToDictionary(b => b.Id, b => b.Name);

                // Get GiftBox information for each item
                var itemsWithGiftBox = new List<CartItemWithGiftBoxResponse>();
                
                foreach (var orderDetail in cart.OrderDetails)
                {
                    var boxTypeName = boxTypeMap.TryGetValue(orderDetail.BoxTypeId, out var name) ? name : string.Empty;
                    
                    var item = new CartItemWithGiftBoxResponse
                    {
                        Id = orderDetail.Id,
                        BoxTypeId = orderDetail.BoxTypeId,
                        BoxTypeName = boxTypeName,
                        Quantity = orderDetail.Quantity,
                        UnitPrice = orderDetail.UnitPrice
                    };

                    // Check if this is a GiftBox item
                    if (boxTypeName == "Gift Box")
                    {
                        _logger.LogInformation("🔍 Found GiftBox item, searching for GiftBoxOrder for cart {CartId}", cart.Id);
                        
                        var giftBoxOrder = await _unitOfWork.GiftBoxOrderRepository
                            .FirstOrDefaultAsync(g => g.OrderId == cart.Id);
                        
                        if (giftBoxOrder != null)
                        {
                            _logger.LogInformation("✅ Found GiftBoxOrder {GiftBoxOrderId} for cart {CartId}", giftBoxOrder.Id, cart.Id);
                            item.GiftBoxOrderId = giftBoxOrder.Id;
                            item.Vegetables = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(giftBoxOrder.Vegetables) ?? new List<string>();
                            item.GreetingMessage = giftBoxOrder.GreetingMessage;
                            item.BoxDescription = giftBoxOrder.BoxDescription;
                            item.LetterScription = giftBoxOrder.LetterScription;
                        }
                        else
                        {
                            _logger.LogWarning("❌ No GiftBoxOrder found for cart {CartId}", cart.Id);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("📦 Item {BoxTypeName} is not a GiftBox", boxTypeName);
                    }

                    itemsWithGiftBox.Add(item);
                }

                var response = new CartResponseWithGiftBox
                {
                    Id = cart.Id,
                    TotalPrice = cart.TotalPrice,
                    FinalPrice = cart.FinalPrice,
                    Items = itemsWithGiftBox
                };

                _logger.LogInformation("✅ Cart with GiftBox info retrieved for user {UserId} with {ItemCount} items", 
                    userId, itemsWithGiftBox.Count);
                return ApiResult<CartResponseWithGiftBox>.Success(response, "Lấy giỏ hàng với thông tin GiftBox thành công!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get cart with GiftBox info for user {UserId}", userId);
                return ApiResult<CartResponseWithGiftBox>.Failure(new Exception("Có lỗi khi lấy giỏ hàng, nội dung lỗi: "+ex.Message));
            }
        }

        // ====================== ADD ITEM ======================
        public async Task<ApiResult<CartResponse>> AddItemAsync(Guid userId, AddItemDto dto)
        {
            try
            {
                _logger.LogInformation("🛒 Adding item to cart for user {UserId}: BoxType {BoxTypeId}, Quantity {Quantity}", 
                    userId, dto.BoxTypeId, dto.Quantity);
                
                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    // 1. Lấy customer
                    _logger.LogDebug("🔍 Validating customer for user {UserId}", userId);
                    var customer = await _unitOfWork.CustomerRepository
                        .FirstOrDefaultAsync(c => c.UserId == userId);
                    if (customer == null)
                    {
                        _logger.LogWarning("❌ Customer not found for user {UserId}", userId);
                        return ApiResult<CartResponse>.Failure(
                            new Exception($"Không tìm thấy khách hàng với ID: {userId}"));
                    }

                    if (dto.Quantity <= 0)
                    {
                        _logger.LogWarning("❌ Invalid quantity for user {UserId}: {Quantity}", userId, dto.Quantity);
                        return ApiResult<CartResponse>.Failure(
                            new Exception("Số lượng phải lớn hơn 0"));
                    }
                    
                    if (dto.Quantity > 1000) // Giới hạn số lượng hợp lý
                    {
                        _logger.LogWarning("❌ Quantity too high for user {UserId}: {Quantity}", userId, dto.Quantity);
                        return ApiResult<CartResponse>.Failure(
                            new Exception("Số lượng không được vượt quá 1000"));
                    }

                    // 2. Tìm giỏ hàng hiện có
                    _logger.LogDebug("🔍 Looking for existing cart for user {UserId}", userId);
                    var existingCart = await _unitOfWork.OrderRepository
                        .FirstOrDefaultAsync(
                            o => o.UserId == userId && o.Status == OrderStatus.Cart,
                            includes: o => o.OrderDetails);
                    var isNewCart = existingCart == null;
                    
                    if (isNewCart)
                    {
                        _logger.LogInformation("🆕 Creating new cart for user {UserId}", userId);
                    }
                    else
                    {
                        _logger.LogInformation("📦 Found existing cart {CartId} for user {UserId} with {ItemCount} items", 
                            existingCart.Id, userId, existingCart.OrderDetails?.Count ?? 0);
                    }
                    
                    var cart = existingCart ?? new Order
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Status = OrderStatus.Cart,
                        TotalPrice = 0,
                        FinalPrice = 0,
                        OrderDetails = new List<OrderDetail>(),
                        // Required fields for cart (will be updated during checkout)
                        Address = "Temporary - Will be updated during checkout",
                        DeliveryTo = "Temporary - Will be updated during checkout", 
                        PhoneNumber = "Temporary - Will be updated during checkout",
                        DeliveryMethod = DeliveryMethod.Standard, // Default delivery method
                        PaymentMethod = PaymentMethod.CashOnDelivery, // Default payment method
                        // Weekly Package fields (default values for cart)
                        IsWeeklyPackage = false,
                        WeeklyPackageId = null,
                        ScheduledDeliveryDate = null,
                        // BaseEntity fields
                        CreatedAt = _currentTime.GetVietnamTime(),
                        UpdatedAt = _currentTime.GetVietnamTime(),
                        CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                        UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                    };

                    if (isNewCart)
                    {
                        _logger.LogInformation("💾 Saving new cart {CartId} to database", cart.Id);
                        await _unitOfWork.OrderRepository.AddAsync(cart);
                    }

                    // 3. Kiểm tra BoxType
                    _logger.LogDebug("🔍 Validating BoxType {BoxTypeId}", dto.BoxTypeId);
                    var box = await _unitOfWork.BoxTypeRepository.GetByIdAsync(dto.BoxTypeId);
                    if (box == null || box.IsDeleted)
                    {
                        _logger.LogWarning("❌ BoxType not found or deleted: {BoxTypeId}", dto.BoxTypeId);
                        return ApiResult<CartResponse>.Failure(
                            new Exception("BoxType không tồn tại!"));
                    }
                    
                    _logger.LogDebug("✅ Found BoxType {BoxTypeId} with price {Price}", dto.BoxTypeId, box.Price);

                    // 4. Add hoặc update item trong cart
                    var existingItem = cart.OrderDetails
                        .FirstOrDefault(d => d.BoxTypeId == dto.BoxTypeId);

                    if (existingItem != null)
                    {
                        var oldQuantity = existingItem.Quantity;
                        existingItem.Quantity += dto.Quantity;
                        existingItem.UnitPrice = box.Price;
                        _logger.LogInformation("📝 Updated existing item: BoxType {BoxTypeId}, Quantity {OldQuantity} -> {NewQuantity}", 
                            dto.BoxTypeId, oldQuantity, existingItem.Quantity);
                        // Không cần UpdateAsync vì entity đang được track
                    }
                    else
                    {
                        _logger.LogInformation("➕ Adding new item to cart: BoxType {BoxTypeId}, Quantity {Quantity}", 
                            dto.BoxTypeId, dto.Quantity);
                        var detail = new OrderDetail
                        {
                            Id = Guid.NewGuid(),
                            OrderId = cart.Id,
                            BoxTypeId = dto.BoxTypeId,
                            Quantity = dto.Quantity,
                            UnitPrice = box.Price,
                            CreatedAt = _currentTime.GetVietnamTime(),
                            UpdatedAt = _currentTime.GetVietnamTime(),
                            CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                            UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                        };

                        await _unitOfWork.OrderDetailRepository.AddAsync(detail);
                        _logger.LogDebug("✅ OrderDetail added to cart: {OrderDetailId}", detail.Id);
                    }

                    // 5. SaveChanges trước khi tính giá để đảm bảo data consistency
                    _logger.LogDebug("💾 Saving cart changes to database");
                    await _unitOfWork.SaveChangesAsync();

                    // 6. Reload cart để có data mới nhất
                    _logger.LogDebug("🔄 Reloading cart to get fresh data");
                    var freshCart = await _unitOfWork.OrderRepository
                        .GetByIdAsync(cart.Id, includes: o => o.OrderDetails);
                    
                    if (freshCart != null)
                    {
                        // Tính lại giá với data mới nhất
                        var oldTotalPrice = freshCart.TotalPrice;
                        freshCart.TotalPrice = freshCart.OrderDetails.Sum(i => i.Quantity * i.UnitPrice);
                        freshCart.FinalPrice = freshCart.TotalPrice;
                        
                        _logger.LogInformation("💰 Cart pricing updated: {OldTotal} -> {NewTotal} VNĐ", 
                            oldTotalPrice, freshCart.TotalPrice);
                        
                        await _unitOfWork.SaveChangesAsync();
                        _logger.LogInformation("✅ Cart {CartId} updated successfully with {ItemCount} items, Total: {TotalPrice} VNĐ", 
                            freshCart.Id, freshCart.OrderDetails?.Count ?? 0, freshCart.TotalPrice);
                    }

                    return ApiResult<CartResponse>.Success(
                        _mapper.Map<CartResponse>(freshCart ?? cart),
                        "Thêm sản phẩm vào giỏ hàng thành công!");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to add item to cart for user {UserId}: BoxType {BoxTypeId}, Quantity {Quantity}", 
                    userId, dto.BoxTypeId, dto.Quantity);
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
                    
                    if (quantity > 1000) // Giới hạn số lượng hợp lý
                        return ApiResult<CartResponse>.Failure(new Exception("Số lượng không được vượt quá 1000"));

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
                        // Không cần UpdateAsync vì entity đang được track
                    }

                    cart.TotalPrice = cart.OrderDetails.Sum(i => i.Quantity * i.UnitPrice);
                    cart.FinalPrice = cart.TotalPrice;

                    // Nếu giỏ hàng trống sau khi cập nhật, xóa luôn cart
                    if (!cart.OrderDetails.Any())
                    {
                        await _unitOfWork.OrderRepository.DeleteAsync(cart.Id);
                        return ApiResult<CartResponse>.Success(new CartResponse 
                        { 
                            Id = Guid.Empty, 
                            TotalPrice = 0, 
                            FinalPrice = 0, 
                            Items = new List<CartItemResponse>() 
                        }, "Giỏ hàng đã được xóa vì không còn sản phẩm nào!");
                    }

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

                    // Nếu giỏ hàng trống sau khi xóa, xóa luôn cart
                    if (!cart.OrderDetails.Any())
                    {
                        await _unitOfWork.OrderRepository.DeleteAsync(cart.Id);
                    }

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

                    // Xóa luôn cart vì không còn item nào
                    await _unitOfWork.OrderRepository.DeleteAsync(cart.Id);
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
                    
                    // Update delivery information
                    cart.Address = dto.Address;
                    cart.DeliveryTo = dto.DeliveryTo;
                    cart.PhoneNumber = dto.PhoneNumber;
                    
                    // Update allergy and preference notes
                    cart.AllergyNote = dto.AllergyNote;
                    cart.PreferenceNote = dto.PreferenceNote;

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

                    // Không cần UpdateAsync vì entity đang được track
                    await _unitOfWork.SaveChangesAsync();

                    // Gửi email xác nhận đơn hàng cho khách hàng và thông báo cho admin
                    try
                    {
                        var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                        if (user != null)
                        {
                            // Gửi email xác nhận cho khách hàng
                            await _emailService.SendOrderConfirmationEmailAsync(user.Email, cart);
                            
                            // Gửi thông báo cho admin
                            await _emailService.SendNewOrderNotificationToAdminAsync(cart);
                            
                            // Gửi cảnh báo đơn hàng giá trị cao (ngưỡng 1 triệu VNĐ)
                            if (cart.FinalPrice > 1000000)
                            {
                                await _emailService.SendHighValueOrderAlertAsync(cart, 1000000);
                            }
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Log lỗi email nhưng không làm fail transaction
                        // Có thể log vào file hoặc database
                    }

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
