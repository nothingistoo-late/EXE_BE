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
                if (request.Items == null || !request.Items.Any())
                    return ApiResult<OrderResponse>.Failure(new Exception("Đơn đặt hàng phải có ít nhất 1 sản phẩm!!!"));

                return await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var userExists = await _unitOfWork.UserRepository.AnyAsync(u => u.Id == request.UserId);
                    if (!userExists)
                        return ApiResult<OrderResponse>.Failure(new Exception("Không tìm thấy người dùng với Id : ." + request.UserId));

                    var order = new Order
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.UserId,
                        Status = OrderStatus.Pending,
                        DeliveryMethod = request.DeliveryMethod,
                        PaymentMethod = request.PaymentMethod,
                        OrderDetails = new List<OrderDetail>(),
                        CreatedAt = _currentTime.GetVietnamTime(),
                        UpdatedAt = _currentTime.GetVietnamTime(),
                        CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty,
                        UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty
                    };

                    foreach (var item in request.Items)
                    {
                        if (item.Quantity <= 0)
                            return ApiResult<OrderResponse>.Failure(new Exception($"Số lượng đặt hàng của Boxtype {item.BoxTypeId} không hợp lí, số lượng bạn đặt đang là : " + item.Quantity));

                        var box = await _unitOfWork.BoxTypeRepository.GetByIdAsync(item.BoxTypeId);
                        if (box == null)
                            return ApiResult<OrderResponse>.Failure(new Exception($"BoxType {item.BoxTypeId} không tìm thấy, xin kiểm tra và hãy thử lại!!"));

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

                    order.TotalPrice = order.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);
                    order.FinalPrice = order.TotalPrice;

                    if (!string.IsNullOrWhiteSpace(request.DiscountCode))
                    {
                        order.DiscountCode = request.DiscountCode;
                        var discount = await _unitOfWork.DiscountRepository
                            .GetActiveDiscountByCodeAsync(request.DiscountCode);

                        if (discount == null)
                            return ApiResult<OrderResponse>.Failure(new Exception("Mã giảm giá không tồn tại hoặc đã hết hạn!!"));

                        // Check if user has already used this discount
                        var hasUsedDiscount = await _unitOfWork.UserDiscountRepository
                            .HasUserUsedDiscountAsync(request.UserId, discount.Id);

                        if (hasUsedDiscount)
                            return ApiResult<OrderResponse>.Failure(new Exception("Bạn đã sử dụng mã giảm giá này rồi, hãy thử mã khác nhé!!"));

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

                    await _unitOfWork.OrderRepository.AddAsync(order);
                    await _unitOfWork.SaveChangesAsync();
                    
                    // Theo dõi đơn hàng pending
                    _pendingOrderTrackingService.TrackPendingOrder(order.Id);

                    // Gửi email xác nhận đơn hàng cho khách hàng và thông báo cho admin
                    try
                    {
                        var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId);
                        if (user != null)
                        {
                            // Gửi email xác nhận cho khách hàng
                            await _emailService.SendOrderConfirmationEmailAsync(user.Email, order);
                            
                            // Gửi thông báo cho admin
                            await _emailService.SendNewOrderNotificationToAdminAsync(order);
                            
                            // Gửi cảnh báo đơn hàng giá trị cao (ngưỡng 10 triệu VNĐ)
                            if (order.FinalPrice > 10000000)
                            {
                                await _emailService.SendHighValueOrderAlertAsync(order, 10000000);
                            }
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Log lỗi email nhưng không làm fail transaction
                        // Có thể log vào file hoặc database
                    }

                    var response = _mapper.Map<OrderResponse>(order);
                    return ApiResult<OrderResponse>.Success(response, "Tạo đơn hàng thành công!!.");
                });
            }
            catch (Exception ex)
            {
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

                if (orders == null || orders.Count == 0)
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
    }
}
