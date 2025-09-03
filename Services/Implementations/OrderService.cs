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

namespace Services.Implementations
{
    public class OrderService : BaseService<Order, Guid>, IOrderService
    {
        private readonly IMapper _mapper;
        public OrderService(IMapper mapper, IGenericRepository<Order, Guid> repository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ICurrentTime currentTime) : base(repository, currentUserService, unitOfWork, currentTime)
        {
            _mapper = mapper;
        }

        public async Task<ApiResult<OrderResponse>> CreateOrderAsync(CreateOrderRequest request)
        {
            try
            {
                if (request.Items == null || !request.Items.Any())
                    return ApiResult<OrderResponse>.Failure(new Exception("Order must have at least one item."));

                var userExists = await _unitOfWork.UserRepository.AnyAsync(u => u.Id == request.UserId);
                if (!userExists)
                    return ApiResult<OrderResponse>.Failure(new Exception("User not found."));

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId, // sửa lại cho khớp entity
                    Status = OrderStatus.Pending,
                    DeliveryMethod = request.DeliveryMethod,
                    PaymentMethod = request.PaymentMethod,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0)
                        return ApiResult<OrderResponse>.Failure(new Exception($"Invalid quantity for BoxType {item.BoxTypeId}"));

                    var box = await _unitOfWork.BoxTypeRepository.GetByIdAsync(item.BoxTypeId);
                    if (box == null)
                        return ApiResult<OrderResponse>.Failure(new Exception($"BoxType {item.BoxTypeId} not found."));

                    order.OrderDetails.Add(new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        BoxTypeId = item.BoxTypeId,
                        Quantity = item.Quantity,
                        UnitPrice = box.Price
                    });
                }

                // Tính giá gốc
                order.TotalPrice = order.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);
                order.FinalPrice = order.TotalPrice;
                // Check discount (nếu có)
                if (!string.IsNullOrWhiteSpace(request.DiscountCode))
                {
                    order.DiscountCode = request.DiscountCode;
                    var discount = await _unitOfWork.DiscountRepository
                        .FirstOrDefaultAsync(d => d.Code == request.DiscountCode && d.IsActive);

                    if (discount == null)
                        return ApiResult<OrderResponse>.Failure(new Exception("Invalid or inactive discount code."));

                    order.FinalPrice = discount.IsPercentage
                        ? order.TotalPrice * (1 - discount.DiscountValue / 100)
                        : order.TotalPrice - discount.DiscountValue;

                    if (order.FinalPrice < 0)
                        order.FinalPrice = 0;
                }

                await _unitOfWork.OrderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // Map sang DTO
                var response = _mapper.Map<OrderResponse>(order);

                return ApiResult<OrderResponse>.Success(response, "Order created successfully.");
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

                if (orders == null || orders.Count == 0)
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
                var orders = await _unitOfWork.OrderRepository.GetAllAsync(null, includes: o=> o.OrderDetails);

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


        //public async Task<ApiResult<Order>> UpdateOrderAsync(Guid id, UpdateOrderRequest request)
        //{
        //    try
        //    {
        //        var order = await _context.Orders
        //            .Include(o => o.OrderDetails)
        //            .FirstOrDefaultAsync(o => o.Id == id);

        //        if (order == null)
        //            return ApiResult<Order>.Failure(new Exception("Order not found."));

        //        order.Status = request.Status;
        //        order.DeliveryMethod = request.DeliveryMethod;
        //        order.PaymentMethod = request.PaymentMethod;

        //        _context.OrderDetails.RemoveRange(order.OrderDetails);
        //        order.OrderDetails.Clear();

        //        foreach (var item in request.Items)
        //        {
        //            if (item.Quantity <= 0)
        //                return ApiResult<Order>.Failure(new Exception($"Invalid quantity for BoxType {item.BoxTypeId}"));

        //            var box = await _context.BoxTypes.FindAsync(item.BoxTypeId);
        //            if (box == null)
        //                return ApiResult<Order>.Failure(new Exception($"BoxType {item.BoxTypeId} not found."));

        //            order.OrderDetails.Add(new OrderDetail
        //            {
        //                Id = Guid.NewGuid(),
        //                OrderId = order.Id,
        //                BoxTypeId = item.BoxTypeId,
        //                Quantity = item.Quantity,
        //                UnitPrice = box.Price
        //            });
        //        }

        //        order.TotalPrice = order.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);
        //        order.FinalPrice = order.TotalPrice;

        //        if (!string.IsNullOrEmpty(request.DiscountCode))
        //        {
        //            var discount = await _context.Discounts
        //                .FirstOrDefaultAsync(d => d.Code == request.DiscountCode && d.IsActive);

        //            if (discount == null)
        //                return ApiResult<Order>.Failure(new Exception("Invalid or inactive discount code."));

        //            order.DiscountId = discount.Id;
        //            order.FinalPrice = discount.IsPercentage
        //                ? order.TotalPrice * (1 - discount.DiscountValue / 100)
        //                : order.TotalPrice - discount.DiscountValue;

        //            if (order.FinalPrice < 0) order.FinalPrice = 0;
        //        }

        //        await _context.SaveChangesAsync();
        //        return ApiResult<Order>.Success(order, "Order updated successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResult<Order>.Failure(ex);
        //    }
        //}

        public async Task<ApiResult<OrderResponse>> CancelledOrderAsync(Guid id)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(id);
                if (order == null)
                    return ApiResult<OrderResponse>.Failure(new Exception("Không tìm thấy đơn hàng này: " + id));

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
                            Message = "Order not found."
                        });
                        continue;
                    }

                    try
                    {
                        order.Status = status;
                        order.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.OrderRepository.UpdateAsync(order);

                        results.Add(new UpdateOrderStatusResult
                        {
                            OrderId = id,
                            IsSuccess = true,
                            Message = "Updated successfully.",
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
                return ApiResult<List<UpdateOrderStatusResult>>.Success(results, "Batch update completed.");
            }
            catch (Exception ex)
            {
                return ApiResult<List<UpdateOrderStatusResult>>.Failure(ex);
            }
        }


    }
}
