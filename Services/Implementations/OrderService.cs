using AutoMapper;
using DTOs.OrderDTOs.Request;
using DTOs.OrderDTOs.Respond;
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

namespace Services.Implementations
{
    public class OrderService : BaseService<Order, Guid>, IOrderService
    {
        private readonly IMapper _mapper;
        private readonly IEXEGmailService _emailService;
        
        public OrderService(IMapper mapper, IGenericRepository<Order, Guid> repository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ICurrentTime currentTime, IEXEGmailService emailService) : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _mapper = mapper;
            _emailService = emailService;
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
                        CreatedAt = DateTime.UtcNow,
                        OrderDetails = new List<OrderDetail>()
                    };

                    foreach (var item in request.Items)
                    {
                        if (item.Quantity <= 0)
                            return ApiResult<OrderResponse>.Failure(new Exception($"Số lượng đặt hàng của Boxtype {item.BoxTypeId} không hợp lí, số lượng bạn đặt đang là : " + item.Quantity));

                        var box = await _unitOfWork.BoxTypeRepository.GetByIdAsync(item.BoxTypeId);
                        if (box == null)
                            return ApiResult<OrderResponse>.Failure(new Exception($"BoxType {item.BoxTypeId} không tìm thấy, xin kiểm tra và hãy thử lại!!"));

                        order.OrderDetails.Add(new OrderDetail
                        {
                            Id = Guid.NewGuid(),
                            OrderId = order.Id,
                            BoxTypeId = item.BoxTypeId,
                            Quantity = item.Quantity,
                            UnitPrice = box.Price
                        });
                    }

                    order.TotalPrice = order.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);
                    order.FinalPrice = order.TotalPrice;

                    if (!string.IsNullOrWhiteSpace(request.DiscountCode))
                    {
                        order.DiscountCode = request.DiscountCode;
                        var discount = await _unitOfWork.DiscountRepository
                            .FirstOrDefaultAsync(d => d.Code == request.DiscountCode && d.IsActive);

                        if (discount == null)
                            return ApiResult<OrderResponse>.Failure(new Exception("Mã giảm giá không tồn tại hoặc đã hết hạn!!"));

                        order.FinalPrice = discount.IsPercentage
                            ? order.TotalPrice * (1 - discount.DiscountValue / 100)
                            : order.TotalPrice - discount.DiscountValue;

                        if (order.FinalPrice < 0)
                            order.FinalPrice = 0;
                    }

                    await _unitOfWork.OrderRepository.AddAsync(order);
                    await _unitOfWork.SaveChangesAsync();

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
                        order.UpdatedAt = DateTime.UtcNow;
                        
                        // Nếu trạng thái được cập nhật thành Paid, đánh dấu đã thanh toán
                        if (status == OrderStatus.Completed)
                        {
                            order.IsPaid = true;
                        }
                        
                        await _unitOfWork.OrderRepository.UpdateAsync(order);

                        // Gửi email khi thanh toán thành công
                        if (status == OrderStatus.Completed)
                        {
                            try
                            {
                                var user = await _unitOfWork.UserRepository.GetByIdAsync(order.UserId);
                                if (user != null)
                                {
                                    await _emailService.SendPaymentSuccessEmailAsync(user.Email, order);
                                }
                            }
                            catch (Exception emailEx)
                            {
                                // Log lỗi email nhưng không làm fail transaction
                            }
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


    }
}
