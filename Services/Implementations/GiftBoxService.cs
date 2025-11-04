using AutoMapper;
using BusinessObjects;
using BusinessObjects.Common;
using DTOs.GiftBoxDTOs.Request;
using DTOs.GiftBoxDTOs.Response;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Extensions;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Interfaces;

namespace Services.Implementations
{
    public class GiftBoxService : BaseService<GiftBoxOrder, Guid>, IGiftBoxService
    {
        private readonly IAIService _aiService;
        private readonly IMapper _mapper;
        private readonly ILogger<GiftBoxService> _logger;
        private new readonly IUnitOfWork _unitOfWork;

        public GiftBoxService(
            IGenericRepository<GiftBoxOrder, Guid> repository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            IAIService aiService,
            IMapper mapper,
            ILogger<GiftBoxService> logger)
            : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _aiService = aiService;
            _mapper = mapper;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResult<GenerateGreetingResponse>> GenerateGreetingAsync(GenerateGreetingRequest request)
        {
            try
            {
                var greetingMessage = await _aiService.GenerateWishAsync(
                    request.Receiver,
                    request.Occasion,
                    request.MainWish,
                    request.CustomMessage);

                var response = new GenerateGreetingResponse
                {
                    GreetingMessage = greetingMessage
                };

                return ApiResult<GenerateGreetingResponse>.Success(response, "Greeting message generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating greeting message");
                return ApiResult<GenerateGreetingResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<GiftBoxOrderResponse>> CreateGiftBoxOrderAsync(CreateGiftBoxRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return ApiResult<GiftBoxOrderResponse>.Failure(new Exception("Địa chỉ giao hàng là bắt buộc"));
                }

                if (string.IsNullOrWhiteSpace(request.DeliveryTo))
                {
                    return ApiResult<GiftBoxOrderResponse>.Failure(new Exception("Tên người nhận là bắt buộc"));
                }

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return ApiResult<GiftBoxOrderResponse>.Failure(new Exception("Số điện thoại là bắt buộc"));
                }

                // Validate phone number format (Vietnamese phone number)
                var phoneRegex = new System.Text.RegularExpressions.Regex(@"^(0|\+84)[1-9][0-9]{8,9}$");
                if (!phoneRegex.IsMatch(request.PhoneNumber.Trim()))
                {
                    return ApiResult<GiftBoxOrderResponse>.Failure(new Exception("Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại theo định dạng: 0912345678 hoặc +84912345678"));
                }

                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    // 1. Validate user exists
                    var userExists = await _unitOfWork.UserRepository.AnyAsync(u => u.Id == request.UserId);
                    if (!userExists)
                        return ApiResult<GiftBoxOrderResponse>.Failure(new Exception("User not found"));

                    // 2. Get GiftBox type
                    var giftBox = await _unitOfWork.BoxTypeRepository
                        .FirstOrDefaultAsync(b => b.Name == "Gift Box");
                    if (giftBox == null)
                        return ApiResult<GiftBoxOrderResponse>.Failure(new Exception("Gift Box type not found"));

                    // 3. Create order
                    var order = new Order
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.UserId,
                        Status = OrderStatus.Pending,
                        DeliveryMethod = request.DeliveryMethod,
                        PaymentMethod = request.PaymentMethod,
                        Address = request.Address.Trim(),
                        DeliveryTo = request.DeliveryTo.Trim(),
                        PhoneNumber = request.PhoneNumber.Trim(),
                        OrderDetails = new List<OrderDetail>(),
                        CreatedAt = _currentTime.GetVietnamTime(),
                        UpdatedAt = _currentTime.GetVietnamTime(),
                        CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                        UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                    };

                    // 4. Add order detail for GiftBox
                    var orderDetail = new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        BoxTypeId = giftBox.Id,
                        Quantity = request.Quantity,
                        UnitPrice = giftBox.Price,
                        CreatedAt = _currentTime.GetVietnamTime(),
                        UpdatedAt = _currentTime.GetVietnamTime(),
                        CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                        UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                    };
                    order.OrderDetails.Add(orderDetail);

                    // 6. Calculate total price
                    order.TotalPrice = order.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);
                    order.FinalPrice = order.TotalPrice;

                    // 7. Apply discount if provided
                    if (!string.IsNullOrWhiteSpace(request.DiscountCode))
                    {
                        order.DiscountCode = request.DiscountCode;
                        var discount = await _unitOfWork.DiscountRepository
                            .GetActiveDiscountByCodeAsync(request.DiscountCode);

                        if (discount == null)
                            return ApiResult<GiftBoxOrderResponse>.Failure(new Exception("Mã giảm giá không tồn tại hoặc đã hết hạn!!"));

                        // Check if user has already used this discount
                        var hasUsedDiscount = await _unitOfWork.UserDiscountRepository
                            .HasUserUsedDiscountAsync(request.UserId, discount.Id);

                        if (hasUsedDiscount)
                            return ApiResult<GiftBoxOrderResponse>.Failure(new Exception("Bạn đã sử dụng mã giảm giá này rồi, hãy thử mã khác nhé!!"));

                        // Apply discount
                        order.FinalPrice = discount.IsPercentage
                            ? order.TotalPrice * (1 - discount.DiscountValue / 100)
                            : order.TotalPrice - discount.DiscountValue;

                        if (order.FinalPrice < 0)
                            order.FinalPrice = 0;

                        // Create UserDiscount record
                        var userDiscount = new UserDiscount
                        {
                            Id = Guid.NewGuid(),
                            UserId = request.UserId,
                            DiscountId = discount.Id,
                            UsedAt = _currentTime.GetVietnamTime(),
                            CreatedAt = _currentTime.GetVietnamTime(),
                            UpdatedAt = _currentTime.GetVietnamTime(),
                            CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                            UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                        };

                        await _unitOfWork.UserDiscountRepository.AddAsync(userDiscount);
                    }

                    // 8. Save order
                    await _unitOfWork.OrderRepository.AddAsync(order);
                    await _unitOfWork.SaveChangesAsync();

                    // 9. Create GiftBoxOrder
                    var giftBoxOrder = new GiftBoxOrder
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        Vegetables = JsonConvert.SerializeObject(request.Vegetables),
                        GreetingMessage = request.GreetingMessage,
                        BoxDescription = request.BoxDescription,
                        LetterScription = request.LetterScription,
                        Address = request.Address,
                        DeliveryTo = request.DeliveryTo,
                        PhoneNumber = request.PhoneNumber
                    };

                    await CreateAsync(giftBoxOrder);
                    await _unitOfWork.SaveChangesAsync();

                    // 10. Create response
                    var response = new GiftBoxOrderResponse
                    {
                        OrderId = order.Id,
                        GiftBoxOrderId = giftBoxOrder.Id,
                        Vegetables = request.Vegetables,
                        GreetingMessage = request.GreetingMessage,
                        BoxDescription = request.BoxDescription,
                        LetterScription = request.LetterScription,
                        Address = request.Address,
                        DeliveryTo = request.DeliveryTo,
                        PhoneNumber = request.PhoneNumber,
                        TotalPrice = order.TotalPrice,
                        FinalPrice = order.FinalPrice,
                        CreatedAt = _currentTime.GetVietnamTime(),
                        Status = order.Status.ToString()
                    };

                    return ApiResult<GiftBoxOrderResponse>.Success(response, "Gift box order created successfully");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gift box order");
                return ApiResult<GiftBoxOrderResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<AddGiftBoxToCartResponse>> AddGiftBoxToCartAsync(AddGiftBoxToCartRequest request)
        {
            try
            {
                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    // 1. Validate user exists
                    var userExists = await _unitOfWork.UserRepository.AnyAsync(u => u.Id == request.UserId);
                    if (!userExists)
                        return ApiResult<AddGiftBoxToCartResponse>.Failure(new Exception("User not found"));

                    // 2. Get GiftBox type
                    var giftBox = await _unitOfWork.BoxTypeRepository
                        .FirstOrDefaultAsync(b => b.Name == "Gift Box");
                    if (giftBox == null)
                        return ApiResult<AddGiftBoxToCartResponse>.Failure(new Exception("Gift Box type not found"));

                    // 3. Find or create cart
                    var cart = await _unitOfWork.OrderRepository
                        .FirstOrDefaultAsync(o => o.UserId == request.UserId && o.Status == OrderStatus.Cart,
                                            includes: o => o.OrderDetails);

                    if (cart == null)
                    {
                        // Create new cart
                        cart = new Order
                        {
                            Id = Guid.NewGuid(),
                            UserId = request.UserId,
                            Status = OrderStatus.Cart,
                            OrderDetails = new List<OrderDetail>(),
                            Address = string.Empty, // Will be filled during checkout
                            DeliveryTo = string.Empty, // Will be filled during checkout
                            PhoneNumber = string.Empty, // Will be filled during checkout
                            PaymentMethod = PaymentMethod.CashOnDelivery, // Default to Cash, will be updated during checkout
                            CreatedAt = _currentTime.GetVietnamTime(),
                            UpdatedAt = _currentTime.GetVietnamTime(),
                            CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                            UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                        };
                        await _unitOfWork.OrderRepository.AddAsync(cart);
                        await _unitOfWork.SaveChangesAsync(); // Save cart first
                    }

                    // 4. Add order detail for GiftBox
                    var orderDetail = new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = cart.Id,
                        BoxTypeId = giftBox.Id,
                        Quantity = request.Quantity,
                        UnitPrice = giftBox.Price,
                        CreatedAt = _currentTime.GetVietnamTime(),
                        UpdatedAt = _currentTime.GetVietnamTime(),
                        CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                        UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                    };
                    
                    // Add order detail to repository
                    await _unitOfWork.OrderDetailRepository.AddAsync(orderDetail);
                    await _unitOfWork.SaveChangesAsync(); // Save order detail

                    // 5. Reload cart with fresh data to avoid concurrency issues
                    var freshCart = await _unitOfWork.OrderRepository
                        .FirstOrDefaultAsync(o => o.Id == cart.Id,
                                            includes: o => o.OrderDetails);

                    // 6. Calculate total price
                    freshCart.TotalPrice = freshCart.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);
                    freshCart.FinalPrice = freshCart.TotalPrice;
                    freshCart.UpdatedAt = _currentTime.GetVietnamTime();
                    freshCart.UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty;

                    // 7. Update cart
                    await _unitOfWork.SaveChangesAsync();

                    // 7. Create GiftBoxOrder for cart
                    var giftBoxOrder = new GiftBoxOrder
                    {
                        Id = Guid.NewGuid(),
                        OrderId = cart.Id,
                        Vegetables = JsonConvert.SerializeObject(request.Vegetables),
                        GreetingMessage = request.GreetingMessage,
                        BoxDescription = request.BoxDescription,
                        LetterScription = request.LetterScription,
                        Address = string.Empty, // Will be filled during checkout
                        DeliveryTo = string.Empty, // Will be filled during checkout
                        PhoneNumber = string.Empty // Will be filled during checkout
                    };

                    await CreateAsync(giftBoxOrder);
                    await _unitOfWork.SaveChangesAsync();

                    // 8. Create response
                    var response = new AddGiftBoxToCartResponse
                    {
                        CartId = freshCart.Id,
                        GiftBoxOrderId = giftBoxOrder.Id,
                        Vegetables = request.Vegetables,
                        GreetingMessage = request.GreetingMessage,
                        BoxDescription = request.BoxDescription,
                        LetterScription = request.LetterScription,
                        Quantity = request.Quantity,
                        UnitPrice = giftBox.Price,
                        TotalPrice = freshCart.TotalPrice,
                        CreatedAt = _currentTime.GetVietnamTime(),
                        Status = freshCart.Status.ToString()
                    };

                    return ApiResult<AddGiftBoxToCartResponse>.Success(response, "Gift box added to cart successfully");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding gift box to cart");
                return ApiResult<AddGiftBoxToCartResponse>.Failure(ex);
            }
        }
    }
}
