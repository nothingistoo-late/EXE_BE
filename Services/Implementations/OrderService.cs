using AutoMapper;
using DTOs.OrderDTOs.Request;
using DTOs.OrderDTOs.Respond;
using DTOs.PayOSDTOs;
using Microsoft.EntityFrameworkCore;
using Services.Commons;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.WorkSeeds.Extensions;
using Services.Commons.Gmail;
using Microsoft.AspNetCore.Http;
using BusinessObjects.Common;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class OrderService : BaseService<Order, Guid>, IOrderService
    {
        private readonly IMapper _mapper;
        private readonly IEXEGmailService _emailService;
        private readonly IPendingOrderTrackingService _pendingOrderTrackingService;
        private readonly IPayOSService _payOSService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<OrderService> _logger;
        
        public OrderService(IMapper mapper, IGenericRepository<Order, Guid> repository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ICurrentTime currentTime, IEXEGmailService emailService, IPendingOrderTrackingService pendingOrderTrackingService, IPayOSService payOSService, IHttpContextAccessor httpContextAccessor, ILogger<OrderService> logger) : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _mapper = mapper;
            _emailService = emailService;
            _pendingOrderTrackingService = pendingOrderTrackingService;
            _payOSService = payOSService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<ApiResult<OrderResponse>> CreateOrderAsync(CreateOrderRequest request)
        {
            try
            {
                _logger.LogInformation("🛒 Starting order creation for user {UserId} with {ItemCount} items", 
                    request.UserId, request.Items?.Count ?? 0);

                if (request.Items == null || !request.Items.Any())
                {
                    _logger.LogWarning("❌ Order creation failed: No items provided for user {UserId}", request.UserId);
                    return ApiResult<OrderResponse>.Failure(new Exception("Đơn đặt hàng phải có ít nhất 1 sản phẩm!!!"));
                }

                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    _logger.LogDebug("🔍 Validating user existence for {UserId}", request.UserId);
                    var userExists = await _unitOfWork.UserRepository.AnyAsync(u => u.Id == request.UserId);
                    if (!userExists)
                    {
                        _logger.LogWarning("❌ User not found: {UserId}", request.UserId);
                        return ApiResult<OrderResponse>.Failure(new Exception("Không tìm thấy người dùng với Id : ." + request.UserId));
                    }

                    var order = new Order
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.UserId,
                        Status = OrderStatus.Pending,
                        DeliveryMethod = request.DeliveryMethod,
                        PaymentMethod = request.PaymentMethod,
                        Address = request.Address,
                        DeliveryTo = request.DeliveryTo,
                        PhoneNumber = request.PhoneNumber,
                        OrderDetails = new List<OrderDetail>(),
                        CreatedAt = _currentTime.GetVietnamTime(),
                        UpdatedAt = _currentTime.GetVietnamTime(),
                        CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                        UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                    };

                    _logger.LogInformation("📦 Creating order {OrderId} for user {UserId}", order.Id, request.UserId);

                    foreach (var item in request.Items)
                    {
                        _logger.LogDebug("🔍 Processing item: BoxType {BoxTypeId}, Quantity {Quantity}", 
                            item.BoxTypeId, item.Quantity);

                        if (item.Quantity <= 0)
                        {
                            _logger.LogWarning("❌ Invalid quantity for BoxType {BoxTypeId}: {Quantity}", 
                                item.BoxTypeId, item.Quantity);
                            return ApiResult<OrderResponse>.Failure(new Exception($"Số lượng đặt hàng của Boxtype {item.BoxTypeId} không hợp lí, số lượng bạn đặt đang là : " + item.Quantity));
                        }

                        var box = await _unitOfWork.BoxTypeRepository.GetByIdAsync(item.BoxTypeId);
                        if (box == null)
                        {
                            _logger.LogWarning("❌ BoxType not found: {BoxTypeId}", item.BoxTypeId);
                            return ApiResult<OrderResponse>.Failure(new Exception($"BoxType {item.BoxTypeId} không tìm thấy, xin kiểm tra và hãy thử lại!!"));
                        }

                        _logger.LogDebug("✅ Found BoxType {BoxTypeId} with price {Price}", item.BoxTypeId, box.Price);

                        var orderDetail = new OrderDetail
                        {
                            Id = Guid.NewGuid(),
                            OrderId = order.Id,
                            BoxTypeId = item.BoxTypeId,
                            Quantity = item.Quantity,
                            UnitPrice = box.Price,
                            CreatedAt = _currentTime.GetVietnamTime(),
                            UpdatedAt = _currentTime.GetVietnamTime(),
                            CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                            UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                        };
                        order.OrderDetails.Add(orderDetail);
                        _logger.LogDebug("✅ Added order detail: {BoxTypeId} x {Quantity} = {SubTotal}", 
                            item.BoxTypeId, item.Quantity, box.Price * item.Quantity);
                    }

                    order.TotalPrice = order.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);
                    order.FinalPrice = order.TotalPrice;

                    _logger.LogInformation("💰 Order {OrderId} total price: {TotalPrice} VNĐ", 
                        order.Id, order.TotalPrice);

                    if (!string.IsNullOrWhiteSpace(request.DiscountCode))
                    {
                        _logger.LogInformation("🎫 Applying discount code: {DiscountCode} for order {OrderId}", 
                            request.DiscountCode, order.Id);
                        
                        order.DiscountCode = request.DiscountCode;
                        var discount = await _unitOfWork.DiscountRepository
                            .GetActiveDiscountByCodeAsync(request.DiscountCode);

                        if (discount == null)
                        {
                            _logger.LogWarning("❌ Invalid discount code: {DiscountCode}", request.DiscountCode);
                            return ApiResult<OrderResponse>.Failure(new Exception("Mã giảm giá không tồn tại hoặc đã hết hạn!!"));
                        }

                        _logger.LogDebug("✅ Found valid discount: {DiscountCode}, Value: {DiscountValue}%", 
                            request.DiscountCode, discount.DiscountValue);

                        // Check if user has already used this discount
                        var hasUsedDiscount = await _unitOfWork.UserDiscountRepository
                            .HasUserUsedDiscountAsync(request.UserId, discount.Id);

                        if (hasUsedDiscount)
                        {
                            _logger.LogWarning("❌ User {UserId} already used discount {DiscountCode}", 
                                request.UserId, request.DiscountCode);
                            return ApiResult<OrderResponse>.Failure(new Exception("Bạn đã sử dụng mã giảm giá này rồi, hãy thử mã khác nhé!!"));
                        }

                        // Apply discount
                        var originalPrice = order.FinalPrice;
                        order.FinalPrice = discount.IsPercentage
                            ? order.TotalPrice * (1 - discount.DiscountValue / 100)
                            : order.TotalPrice - discount.DiscountValue;

                        if (order.FinalPrice < 0)
                            order.FinalPrice = 0;

                        _logger.LogInformation("🎫 Discount applied: {OriginalPrice} -> {FinalPrice} (Saved: {Savings})", 
                            originalPrice, order.FinalPrice, originalPrice - order.FinalPrice);

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
                        _logger.LogDebug("✅ UserDiscount record created for user {UserId}", request.UserId);
                    }

                    _logger.LogInformation("💾 Saving order {OrderId} to database", order.Id);
                    await _unitOfWork.OrderRepository.AddAsync(order);
                    await _unitOfWork.SaveChangesAsync();
                    
                    _logger.LogInformation("📊 Order {OrderId} saved successfully. Final price: {FinalPrice} VNĐ", 
                        order.Id, order.FinalPrice);
                    
                    // Theo dõi đơn hàng pending
                    _pendingOrderTrackingService.TrackPendingOrder(order.Id);
                    _logger.LogDebug("📈 Order {OrderId} added to pending tracking", order.Id);

                    // Gửi email xác nhận đơn hàng cho khách hàng và thông báo cho admin
                    try
                    {
                        _logger.LogInformation("📧 Sending order confirmation emails for order {OrderId}", order.Id);
                        var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId);
                        if (user != null)
                        {
                            _logger.LogDebug("📧 Sending confirmation email to {Email}", user.Email);
                            // Gửi email xác nhận cho khách hàng
                            await _emailService.SendOrderConfirmationEmailAsync(user.Email, order);
                            
                            _logger.LogDebug("📧 Sending admin notification for order {OrderId}", order.Id);
                            // Gửi thông báo cho admin
                            await _emailService.SendNewOrderNotificationToAdminAsync(order);
                            
                            // Gửi cảnh báo đơn hàng giá trị cao (ngưỡng 10 triệu VNĐ)
                            if (order.FinalPrice > 1000000)
                            {
                                _logger.LogWarning("💰 High value order detected: {OrderId} - {FinalPrice} VNĐ", 
                                    order.Id, order.FinalPrice);
                                await _emailService.SendHighValueOrderAlertAsync(order, 1000000);
                            }
                            
                            _logger.LogInformation("✅ All emails sent successfully for order {OrderId}", order.Id);
                        }
                        else
                        {
                            _logger.LogWarning("❌ User not found for email notification: {UserId}", request.UserId);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "❌ Failed to send emails for order {OrderId}", order.Id);
                        // Log lỗi email nhưng không làm fail transaction
                        // Có thể log vào file hoặc database
                    }

                    _logger.LogInformation("🎉 Order {OrderId} created successfully for user {UserId}", 
                        order.Id, request.UserId);
                    var response = _mapper.Map<OrderResponse>(order);
                    return ApiResult<OrderResponse>.Success(response, "Tạo đơn hàng thành công!!.");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create order for user {UserId}", request.UserId);
                return ApiResult<OrderResponse>.Failure(ex);
            }
        }


        public async Task<ApiResult<OrderResponse>> GetOrderByIdAsync(Guid id)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(id);

                if (order == null)
                    return ApiResult<OrderResponse>.Failure(new Exception("Không tìm thấy đơn hàng với id : "+ id));

                var response = _mapper.Map<OrderResponse>(order);

                return ApiResult<OrderResponse>.Success(response, "Lấy đơn hàng theo id thành công!!!");
            }
            catch (Exception ex)
            {
                return ApiResult<OrderResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<List<OrderResponse>>> GetAllOrdersByCustomerIDAsync(Guid customerId)
        {
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(customerId);
                if (user == null)
                    return ApiResult<List<OrderResponse>>.Failure(new Exception("Không tìm thấy khách hàng này : " + customerId));

                var orders = await _unitOfWork.OrderRepository.GetAllOrdersByCustomerIdAsync(customerId);

                if (orders == null)
                    return ApiResult<List<OrderResponse>>.Failure(new Exception("Không tìm thấy đơn hàng nào cho khách hàng này : " + customerId));

                // loại bỏ đơn hàng có trạng thái Cart
                orders = orders.Where(o => o.Status != OrderStatus.Cart).ToList();

                if (orders.Count == 0)
                    return ApiResult<List<OrderResponse>>.Failure(new Exception("Không tìm thấy đơn hàng nào cho khách hàng này : " + customerId));

                var response = _mapper.Map<List<OrderResponse>>(orders);

                return ApiResult<List<OrderResponse>>.Success(response, "Lấy tất cả đơn hàng theo id khách hàng thành công!!");
            }
            catch (Exception ex)
            {
                return ApiResult<List<OrderResponse>>.Failure(ex);
            }
        }

        public async Task<ApiResult<List<OrderResponse>>> GetAllOrderAsync()
        {
            try
            {
                var orders = await _unitOfWork.OrderRepository.GetAllAsync(o => o.Status != OrderStatus.Cart, includes: o=> o.OrderDetails);

                orders = orders
                    .OrderByDescending(o => o.CreatedAt)
                    .ThenByDescending(o => o.Status) // hoặc ThenByDescending(o => o.Status)
                    .ToList(); if (orders == null || orders.Count == 0)
                    return ApiResult<List<OrderResponse>>.Failure(new Exception("Không tìm thấy đơn hàng nào!!"));

                var response = _mapper.Map<List<OrderResponse>>(orders);

                return ApiResult<List<OrderResponse>>.Success(response, "Lấy tất cả đơn hàng thành công!!");
            }
            catch (Exception ex)
            {
                return ApiResult<List<OrderResponse>>.Failure(ex);
            }
        }

        public async Task<ApiResult<OrderResponse>> CancelledOrderAsync(Guid id)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(id, includes: c=> c.OrderDetails);
                if (order == null)
                    return ApiResult<OrderResponse>.Failure(new Exception("Không tìm thấy đơn hàng này: " + id));
                if (order.Status == OrderStatus.Cancelled)
                    return ApiResult<OrderResponse>.Failure(new Exception("Đơn hàng đã bị hủy trước đó rồi!!!"));
                if (order.IsDelivered)
                    return ApiResult<OrderResponse>.Failure(new Exception("Đơn hàng đã được giao, không thể hủy!!!"));
                order.Status = OrderStatus.Cancelled;
                await _unitOfWork.SaveChangesAsync();
                var responds = _mapper.Map<OrderResponse>(order);
                return ApiResult<OrderResponse>.Success(responds, "Hủy đơn hàng thành công!!!");
            }
            catch (Exception ex)
            {
                return ApiResult<OrderResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<List<UpdateOrderStatusResult>>> UpdateOrderStatusAsync(List<Guid> guids, OrderStatus status)
        {
            var results = new List<UpdateOrderStatusResult>();

            try
            {
                foreach (var id in guids)
                {
                    var order = await _unitOfWork.OrderRepository.GetByIdAsync(id);
                    if (order == null)
                    {
                        results.Add(new UpdateOrderStatusResult
                        {
                            OrderId = id,
                            IsSuccess = false,
                            Message = "Không tìm thấy đơn hàng với Id : "+ id
                        });
                        continue;
                    }

                    try
                    {
                        order.Status = status;
                        order.UpdatedAt = _currentTime.GetVietnamTime();
                        
                        // Nếu trạng thái được cập nhật thành Paid, đánh dấu đã thanh toán
                        if (status == OrderStatus.Completed)
                        {
                            order.IsPaid = true;
                        }
                        
                        await _unitOfWork.OrderRepository.UpdateAsync(order);
                        
                        // Xóa khỏi tracking khi đơn hàng không còn pending
                        if (status != OrderStatus.Pending)
                        {
                            _pendingOrderTrackingService.RemovePendingOrder(order.Id);
                        }

                        // Gửi email theo trạng thái đơn hàng
                        try
                        {
                            var user = await _unitOfWork.UserRepository.GetByIdAsync(order.UserId);
                            if (user != null)
                            {
                                switch (status)
                                {
                                    case OrderStatus.Processing:
                                        await _emailService.SendOrderPreparationEmailAsync(user.Email, order);
                                        break;
                                    case OrderStatus.Completed:
                                        await _emailService.SendPaymentSuccessEmailAsync(user.Email, order);
                                        break;
                                    case OrderStatus.Cancelled:
                                        await _emailService.SendOrderCancelledEmailAsync(user.Email, order, "Đơn hàng đã bị hủy bởi hệ thống");
                                        // Gửi cảnh báo cho admin khi đơn hàng bị hủy
                                        await _emailService.SendOrderCancelledAlertAsync(order, "Đơn hàng đã bị hủy bởi hệ thống");
                                        break;
                                }
                            }
                        }
                        catch (Exception emailEx)
                        {
                            // Log lỗi email nhưng không làm fail transaction
                        }

                        results.Add(new UpdateOrderStatusResult
                        {
                            OrderId = id,
                            IsSuccess = true,
                            Message = "Cập nhật trạng thái thành công!!.",
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new UpdateOrderStatusResult
                        {
                            OrderId = id,
                            IsSuccess = false,
                            Message = ex.Message
                        });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                return ApiResult<List<UpdateOrderStatusResult>>.Success(results, "Cập nhật trạng thái đa đơn hàng thành công!!");
            }
            catch (Exception ex)
            {
                return ApiResult<List<UpdateOrderStatusResult>>.Failure(ex);
            }
        }

        public async Task<ApiResult<PaymentLinkResponse>> CreatePayOSPaymentAsync(Guid orderId)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId, includes: o => o.OrderDetails);
                if (order == null)
                    return ApiResult<PaymentLinkResponse>.Failure(new Exception("Order not found"));

                if (order.PaymentMethod != PaymentMethod.PayOS)
                    return ApiResult<PaymentLinkResponse>.Failure(new Exception("Order is not using PayOS payment method"));

                if (order.IsPaid)
                    return ApiResult<PaymentLinkResponse>.Failure(new Exception("Order is already paid"));

                // Ensure product names are available even if BoxType navigation isn't included
                var boxTypeIds = order.OrderDetails.Select(od => od.BoxTypeId).Distinct().ToList();
                var boxTypes = await _unitOfWork.BoxTypeRepository.GetByIdsAsync(boxTypeIds);
                var boxTypeMap = boxTypes.ToDictionary(b => b.Id, b => b.Name);

                var paymentItems = order.OrderDetails.Select(od => new PaymentItem
                {
                    Name = boxTypeMap.TryGetValue(od.BoxTypeId, out var name) && !string.IsNullOrWhiteSpace(name)
                        ? name
                        : "Unknown Product",
                    Quantity = od.Quantity,
                    Price = (int)od.UnitPrice
                }).ToList();

                var request = new CreatePaymentLinkRequest
                {
                    OrderId = orderId,
                    Amount = (int)order.FinalPrice,
                    Description = $"Payment for Order #{orderId}",
                    Items = paymentItems
                };

                var result = await _payOSService.CreatePaymentLinkAsync(request);
                
                if (result.IsSuccess)
                {
                    // Update order with payment link information
                    order.PayOSPaymentLinkId = result.Data.PaymentLinkId;
                    order.PayOSPaymentUrl = result.Data.PaymentUrl;
                    order.PayOSOrderCode = result.Data.OrderCode.ToString(); // Lưu PayOS OrderCode vào Order
                    order.PaymentStatus = PaymentStatus.Pending;
                    await _unitOfWork.SaveChangesAsync();
                    
                    _logger.LogInformation("PayOS payment link created for order {OrderId} with PayOSOrderCode: {PayOSOrderCode}", 
                        order.Id, order.PayOSOrderCode);
                }

                return result;
            }
            catch (Exception ex)
            {
                return ApiResult<PaymentLinkResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<bool>> ProcessPayOSPaymentAsync(string paymentLinkId, string orderCode)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository
                    .FirstOrDefaultAsync(o => o.PayOSPaymentLinkId == paymentLinkId);

                if (order == null)
                    return ApiResult<bool>.Failure(new Exception("Order not found"));

                // Update order status to paid
                order.IsPaid = true;
                order.PaymentStatus = PaymentStatus.Paid;
                order.Status = OrderStatus.Processing;
                order.UpdatedAt = _currentTime.GetVietnamTime();

                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true, "Payment processed successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure(ex);
            }
        }

        public async Task<ApiResult<OrderResponse>> FindOrderByPayOSOrderCodeAsync(string orderCode)
        {
            try
            {
                _logger.LogInformation("Finding order by PayOSOrderCode: {OrderCode}", orderCode);
                
                // Tìm order bằng PayOSOrderCode
                var order = await _unitOfWork.OrderRepository
                    .FirstOrDefaultAsync(o => o.PayOSOrderCode == orderCode);
                
                if (order != null)
                {
                    _logger.LogInformation("Found order {OrderId} with PayOSOrderCode: {OrderCode}", order.Id, orderCode);
                    var response = _mapper.Map<OrderResponse>(order);
                    return ApiResult<OrderResponse>.Success(response, "Order found by PayOSOrderCode");
                }
                
                // Nếu không tìm thấy, tìm order gần nhất có thể liên quan
                _logger.LogInformation("Order not found with PayOSOrderCode, searching for recent orders...");
                
                var allOrders = await _unitOfWork.OrderRepository
                    .GetAllAsync(o => o.CreatedAt > _currentTime.GetVietnamTime().AddHours(-2));
                
                var recentOrders = allOrders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10);
                
                _logger.LogInformation("Found {Count} recent orders", recentOrders.Count());
                
                foreach (var o in recentOrders)
                {
                    _logger.LogInformation("Recent Order {OrderId}: PayOSOrderCode='{PayOSOrderCode}', PaymentMethod={PaymentMethod}, Status={Status}, CreatedAt={CreatedAt}", 
                        o.Id, o.PayOSOrderCode ?? "NULL", o.PaymentMethod, o.Status, o.CreatedAt);
                }
                
                // Không tự động cập nhật order - chỉ log thông tin để debug
                _logger.LogWarning("No order found with PayOSOrderCode: {OrderCode}", orderCode);
                _logger.LogWarning("Recent orders analysis:");
                
                var ordersWithoutPayOS = recentOrders.Where(o => string.IsNullOrEmpty(o.PayOSOrderCode)).ToList();
                if (ordersWithoutPayOS.Any())
                {
                    _logger.LogWarning("Found {Count} recent orders without PayOSOrderCode:", ordersWithoutPayOS.Count);
                    foreach (var o in ordersWithoutPayOS)
                    {
                        _logger.LogWarning("Order {OrderId}: PaymentMethod={PaymentMethod}, Status={Status}, CreatedAt={CreatedAt}", 
                            o.Id, o.PaymentMethod, o.Status, o.CreatedAt);
                    }
                }
                else
                {
                    _logger.LogWarning("No recent orders found without PayOSOrderCode");
                }
                
                return ApiResult<OrderResponse>.Failure(new Exception($"No order found with PayOSOrderCode: {orderCode}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding order by PayOSOrderCode: {OrderCode}", orderCode);
                return ApiResult<OrderResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<bool>> VerifyOrderPaymentFlowAsync(Guid orderId)
        {
            try
            {
                _logger.LogInformation("Verifying payment flow for order {OrderId}", orderId);
                
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogError("Order {OrderId} not found", orderId);
                    return ApiResult<bool>.Failure(new Exception("Order not found"));
                }
                
                _logger.LogInformation("Order {OrderId} details:", orderId);
                _logger.LogInformation("- PaymentMethod: {PaymentMethod}", order.PaymentMethod);
                _logger.LogInformation("- PayOSOrderCode: {PayOSOrderCode}", order.PayOSOrderCode ?? "NULL");
                _logger.LogInformation("- PayOSPaymentLinkId: {PayOSPaymentLinkId}", order.PayOSPaymentLinkId ?? "NULL");
                _logger.LogInformation("- PayOSPaymentUrl: {PayOSPaymentUrl}", order.PayOSPaymentUrl ?? "NULL");
                _logger.LogInformation("- PaymentStatus: {PaymentStatus}", order.PaymentStatus);
                _logger.LogInformation("- Status: {Status}", order.Status);
                _logger.LogInformation("- IsPaid: {IsPaid}", order.IsPaid);
                
                var issues = new List<string>();
                
                if (order.PaymentMethod != PaymentMethod.PayOS)
                {
                    issues.Add("Order is not using PayOS payment method");
                }
                
                if (string.IsNullOrEmpty(order.PayOSOrderCode))
                {
                    issues.Add("Order does not have PayOSOrderCode - CreatePayOSPaymentAsync may not have been called");
                }
                
                if (string.IsNullOrEmpty(order.PayOSPaymentLinkId))
                {
                    issues.Add("Order does not have PayOSPaymentLinkId");
                }
                
                if (string.IsNullOrEmpty(order.PayOSPaymentUrl))
                {
                    issues.Add("Order does not have PayOSPaymentUrl");
                }
                
                if (issues.Any())
                {
                    _logger.LogError("Payment flow issues found for order {OrderId}:", orderId);
                    foreach (var issue in issues)
                    {
                        _logger.LogError("- {Issue}", issue);
                    }
                    return ApiResult<bool>.Failure(new Exception($"Payment flow issues: {string.Join(", ", issues)}"));
                }
                
                _logger.LogInformation("Payment flow verification passed for order {OrderId}", orderId);
                return ApiResult<bool>.Success(true, "Payment flow verification passed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment flow for order {OrderId}", orderId);
                return ApiResult<bool>.Failure(ex);
            }
        }

        public async Task<ApiResult<OrderResponse>> GetOrderByIdForDebugAsync(Guid orderId)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
                if (order == null)
                    return ApiResult<OrderResponse>.Failure(new Exception("Order not found"));

                _logger.LogInformation("Debug Order {OrderId}: PayOSOrderCode='{PayOSOrderCode}', PaymentMethod={PaymentMethod}, Status={Status}, IsPaid={IsPaid}", 
                    order.Id, order.PayOSOrderCode ?? "NULL", order.PaymentMethod, order.Status, order.IsPaid);

                var response = _mapper.Map<OrderResponse>(order);
                return ApiResult<OrderResponse>.Success(response, "Order debug info retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order debug info for {OrderId}", orderId);
                return ApiResult<OrderResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<bool>> UpdateOrderStatusByOrderCodeAsync(string orderCode, OrderStatus status, object? paymentInfo = null)
        {
            try
            {
                _logger.LogInformation("Searching for order with PayOSOrderCode: {OrderCode}", orderCode);
                
                // Tìm order bằng PayOSOrderCode
                var order = await _unitOfWork.OrderRepository
                    .FirstOrDefaultAsync(o => o.PayOSOrderCode == orderCode);

                if (order == null)
                {
                    _logger.LogError("Order not found with PayOSOrderCode: {OrderCode}", orderCode);
                    _logger.LogError("Possible causes:");
                    _logger.LogError("1. Order was created but CreatePayOSPaymentAsync was never called");
                    _logger.LogError("2. PayOSOrderCode was not saved to database");
                    _logger.LogError("3. Webhook is being called with wrong OrderCode");
                    _logger.LogError("4. Database transaction was not committed");
                    
                    return ApiResult<bool>.Failure(new Exception($"Order not found with order code: {orderCode}"));
                }
                
                _logger.LogInformation("Found order {OrderId} with PayOSOrderCode: {OrderCode}", order.Id, orderCode);
                
                // Cập nhật trạng thái order
                await UpdateOrderStatusInternal(order, status);
                return ApiResult<bool>.Success(true, $"Order status updated to {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status by order code {OrderCode}", orderCode);
                return ApiResult<bool>.Failure(ex);
            }
        }

        private async Task UpdateOrderStatusInternal(Order order, OrderStatus status)
        {
            // Cập nhật trạng thái
            order.Status = status;
            order.UpdatedAt = _currentTime.GetVietnamTime();

            // Xử lý theo trạng thái
            switch (status)
            {
                case OrderStatus.Processing:
                case OrderStatus.Completed:
                    order.IsPaid = true;
                    order.PaymentStatus = PaymentStatus.Paid;
                    break;
                case OrderStatus.Cancelled:
                    order.PaymentStatus = PaymentStatus.Cancelled;
                    break;
                case OrderStatus.Pending:
                    order.PaymentStatus = PaymentStatus.Expired;
                    break;
            }

            // Xóa khỏi tracking khi đơn hàng không còn pending
            if (status != OrderStatus.Pending)
            {
                _pendingOrderTrackingService.RemovePendingOrder(order.Id);
            }

            await _unitOfWork.SaveChangesAsync();

            // Gửi email thông báo (nếu cần)
            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(order.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    switch (status)
                    {
                        case OrderStatus.Processing:
                            await _emailService.SendOrderPreparationEmailAsync(user.Email, order);
                            break;
                        case OrderStatus.Completed:
                            await _emailService.SendPaymentSuccessEmailAsync(user.Email, order);
                            break;
                        case OrderStatus.Cancelled:
                            await _emailService.SendOrderCancelledEmailAsync(user.Email, order, "Đơn hàng đã bị hủy");
                            break;
                    }
                }
                else
                {
                    _logger.LogWarning("Cannot send email for order {OrderId}: User not found or email is null", order.Id);
                }
            }
            catch (Exception emailEx)
            {
                // Log lỗi email nhưng không làm fail transaction
                _logger.LogError(emailEx, "Error sending email for order {OrderId}", order.Id);
            }
        }

        /// <summary>
        /// Tạo gói hàng tuần - 2 đơn hàng với giá ưu đãi 250k thay vì 300k
        /// Mỗi đơn hàng cách nhau 3 ngày
        /// </summary>
        public async Task<ApiResult<WeeklyPackageResponse>> CreateWeeklyPackageAsync(CreateWeeklyPackageRequest request)
        {
            try
            {
                _logger.LogInformation("📦 Starting weekly package creation for user {UserId} with {ItemCount} items", 
                    request.UserId, request.Items?.Count ?? 0);

                // Validate request
                if (request.Items == null || !request.Items.Any())
                {
                    _logger.LogWarning("❌ Weekly package creation failed: No items provided for user {UserId}", request.UserId);
                    return ApiResult<WeeklyPackageResponse>.Failure(new Exception("Gói hàng tuần phải có ít nhất 1 sản phẩm!!!"));
                }

                if (request.DeliveryStartDate < _currentTime.GetVietnamTime().Date)
                {
                    _logger.LogWarning("❌ Invalid delivery start date: {DeliveryStartDate} for user {UserId}", 
                        request.DeliveryStartDate, request.UserId);
                    return ApiResult<WeeklyPackageResponse>.Failure(new Exception("Ngày bắt đầu giao hàng không được trong quá khứ!!!"));
                }

                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    // 1. Validate user exists
                    _logger.LogDebug("🔍 Validating user existence for weekly package: {UserId}", request.UserId);
                    var userExists = await _unitOfWork.UserRepository.AnyAsync(u => u.Id == request.UserId);
                    if (!userExists)
                    {
                        _logger.LogWarning("❌ User not found for weekly package: {UserId}", request.UserId);
                        return ApiResult<WeeklyPackageResponse>.Failure(new Exception("Không tìm thấy người dùng với Id: " + request.UserId));
                    }

                    // 2. Generate unique WeeklyPackageId
                    var weeklyPackageId = Guid.NewGuid();
                    _logger.LogInformation("📦 Generated WeeklyPackageId: {WeeklyPackageId} for user {UserId}", 
                        weeklyPackageId, request.UserId);

                    // 3. Calculate delivery dates (3 days apart)
                    var firstDeliveryDate = request.DeliveryStartDate;
                    var secondDeliveryDate = firstDeliveryDate.AddDays(3);
                    
                    _logger.LogInformation("📅 Weekly package delivery schedule: First: {FirstDate}, Second: {SecondDate}", 
                        firstDeliveryDate.ToString("dd/MM/yyyy"), secondDeliveryDate.ToString("dd/MM/yyyy"));

                    // 4. Calculate pricing
                    _logger.LogDebug("💰 Calculating pricing for weekly package");
                    var normalTotalPrice = 0.0;
                    foreach (var item in request.Items)
                    {
                        var box = await _unitOfWork.BoxTypeRepository.GetByIdAsync(item.BoxTypeId);
                        normalTotalPrice += (box?.Price ?? 0) * item.Quantity;
                        _logger.LogDebug("📦 Item pricing: BoxType {BoxTypeId} x {Quantity} = {SubTotal}", 
                            item.BoxTypeId, item.Quantity, (box?.Price ?? 0) * item.Quantity);
                    }

                    var weeklyPackagePrice = request.WeeklyPackagePrice; // 250k
                    var savings = (normalTotalPrice * 2) - weeklyPackagePrice; // Savings compared to 2 separate orders
                    
                    _logger.LogInformation("💰 Weekly package pricing: Normal total: {NormalTotal}, Package price: {PackagePrice}, Savings: {Savings}", 
                        normalTotalPrice * 2, weeklyPackagePrice, savings);

                    // 5. Create first order (immediate delivery)
                    _logger.LogInformation("📦 Creating first order for weekly package {WeeklyPackageId}", weeklyPackageId);
                    var firstOrder = await CreateWeeklyOrderAsync(
                        request, 
                        weeklyPackageId, 
                        firstDeliveryDate, 
                        weeklyPackagePrice / 2, // Split the package price between two orders
                        "Đơn hàng 1/2 - Gói hàng tuần"
                    );

                    if (!firstOrder.IsSuccess)
                    {
                        _logger.LogError("❌ Failed to create first order for weekly package {WeeklyPackageId}: {Error}", 
                            weeklyPackageId, firstOrder.Exception?.Message);
                        return ApiResult<WeeklyPackageResponse>.Failure(firstOrder.Exception ?? new Exception("Failed to create first order"));
                    }
                    
                    _logger.LogInformation("✅ First order created successfully: {OrderId}", firstOrder.Data?.Id);

                    // 6. Create second order (3 days later)
                    _logger.LogInformation("📦 Creating second order for weekly package {WeeklyPackageId}", weeklyPackageId);
                    var secondOrder = await CreateWeeklyOrderAsync(
                        request, 
                        weeklyPackageId, 
                        secondDeliveryDate, 
                        weeklyPackagePrice / 2, // Split the package price between two orders
                        "Đơn hàng 2/2 - Gói hàng tuần"
                    );

                    if (!secondOrder.IsSuccess)
                    {
                        _logger.LogError("❌ Failed to create second order for weekly package {WeeklyPackageId}: {Error}", 
                            weeklyPackageId, secondOrder.Exception?.Message);
                        return ApiResult<WeeklyPackageResponse>.Failure(secondOrder.Exception ?? new Exception("Failed to create second order"));
                    }
                    
                    _logger.LogInformation("✅ Second order created successfully: {OrderId}", secondOrder.Data?.Id);

                    // 7. Create response
                    _logger.LogInformation("📋 Creating weekly package response for {WeeklyPackageId}", weeklyPackageId);
                    var response = new WeeklyPackageResponse
                    {
                        WeeklyPackageId = weeklyPackageId,
                        TotalPackagePrice = weeklyPackagePrice,
                        Savings = savings,
                        DeliveryStartDate = firstDeliveryDate,
                        SecondDeliveryDate = secondDeliveryDate,
                        Orders = new List<WeeklyOrderResponse>
                        {
                            _mapper.Map<WeeklyOrderResponse>(firstOrder.Data),
                            _mapper.Map<WeeklyOrderResponse>(secondOrder.Data)
                        }
                    };

                    _logger.LogInformation("🎉 Weekly package {WeeklyPackageId} created successfully! Total savings: {Savings:N0} VNĐ", 
                        weeklyPackageId, savings);
                    return ApiResult<WeeklyPackageResponse>.Success(response, 
                        $"Tạo gói hàng tuần thành công! Tiết kiệm được {savings:N0} VNĐ. " +
                        $"Đơn hàng đầu tiên: {firstDeliveryDate:dd/MM/yyyy}, " +
                        $"Đơn hàng thứ hai: {secondDeliveryDate:dd/MM/yyyy}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create weekly package for user {UserId}", request.UserId);
                return ApiResult<WeeklyPackageResponse>.Failure(ex);
            }
        }

        /// <summary>
        /// Helper method để tạo một đơn hàng trong gói hàng tuần
        /// </summary>
        private async Task<ApiResult<OrderResponse>> CreateWeeklyOrderAsync(
            CreateWeeklyPackageRequest request, 
            Guid weeklyPackageId, 
            DateTime deliveryDate, 
            double orderPrice,
            string orderNote)
        {
            try
            {
                // Create order entity
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    Status = OrderStatus.Pending,
                    DeliveryMethod = request.DeliveryMethod,
                    PaymentMethod = request.PaymentMethod,
                    Address = request.Address,
                    DeliveryTo = request.DeliveryTo,
                    PhoneNumber = request.PhoneNumber,
                    IsWeeklyPackage = true,
                    WeeklyPackageId = weeklyPackageId,
                    ScheduledDeliveryDate = deliveryDate,
                    OrderDetails = new List<OrderDetail>(),
                    CreatedAt = _currentTime.GetVietnamTime(),
                    UpdatedAt = _currentTime.GetVietnamTime(),
                    CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                    UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                };

                // Add order details
                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0)
                        return ApiResult<OrderResponse>.Failure(new Exception($"Số lượng đặt hàng của Boxtype {item.BoxTypeId} không hợp lí: {item.Quantity}"));

                    var box = await _unitOfWork.BoxTypeRepository.GetByIdAsync(item.BoxTypeId);
                    if (box == null)
                        return ApiResult<OrderResponse>.Failure(new Exception($"BoxType {item.BoxTypeId} không tìm thấy"));

                    var orderDetail = new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        BoxTypeId = item.BoxTypeId,
                        Quantity = item.Quantity,
                        UnitPrice = box.Price,
                        CreatedAt = _currentTime.GetVietnamTime(),
                        UpdatedAt = _currentTime.GetVietnamTime(),
                        CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                        UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                    };
                    order.OrderDetails.Add(orderDetail);
                }

                // Set pricing
                order.TotalPrice = order.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);
                order.FinalPrice = orderPrice; // Use the split package price

                // Apply discount if provided
                if (!string.IsNullOrWhiteSpace(request.DiscountCode))
                {
                    order.DiscountCode = request.DiscountCode;
                    var discount = await _unitOfWork.DiscountRepository.GetActiveDiscountByCodeAsync(request.DiscountCode);

                    if (discount != null)
                    {
                        // Check if user has already used this discount
                        var hasUsedDiscount = await _unitOfWork.UserDiscountRepository.HasUserUsedDiscountAsync(request.UserId, discount.Id);

                        if (!hasUsedDiscount)
                        {
                            // Apply discount to the package price
                            order.FinalPrice = discount.IsPercentage
                                ? orderPrice * (1 - discount.DiscountValue / 100)
                                : orderPrice - discount.DiscountValue;

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
                    }
                }

                // Save order
                await _unitOfWork.OrderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // Track pending order
                _pendingOrderTrackingService.TrackPendingOrder(order.Id);

                // Send email notification
                try
                {
                    var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId);
                    if (user != null)
                    {
                        await _emailService.SendOrderConfirmationEmailAsync(user.Email, order);
                    }
                }
                catch (Exception emailEx)
                {
                    // Log email error but don't fail the transaction
                    _logger.LogError(emailEx, "Error sending email for weekly package order {OrderId}", order.Id);
                }

                var response = _mapper.Map<OrderResponse>(order);
                return ApiResult<OrderResponse>.Success(response, $"Tạo {orderNote} thành công");
            }
            catch (Exception ex)
            {
                return ApiResult<OrderResponse>.Failure(ex);
            }
        }
    }
}
