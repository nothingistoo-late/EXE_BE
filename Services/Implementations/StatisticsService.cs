using Microsoft.EntityFrameworkCore;
using Repositories;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class StatisticsService : IStatisticsService
    {
        private readonly EXE_BE _context;
        public StatisticsService(EXE_BE context)
        {
            _context = context;
        }
        public async Task<ApiResult<OrderStatisticsResponse>> GetOrderStatisticsAsync(OrderStatisticsRequest request)
        {
            try
            {
                var query = _context.Orders
                    .Where(o => o.CreatedAt >= request.StartDate && o.CreatedAt <= request.EndDate)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.BoxType)
                    .Include(o => o.User);

                var orders = await query.ToListAsync();

                if (!orders.Any())
                {
                    return ApiResult<OrderStatisticsResponse>.Success(
                        new OrderStatisticsResponse(),
                        "Không có đơn hàng nào trong khoảng thời gian này");
                }

                var response = new OrderStatisticsResponse
                {
                    // Tổng quan
                    TotalOrders = orders.Count,
                    PaidOrders = orders.Count(o => o.IsPaid),
                    DeliveredOrders = orders.Count(o => o.IsDelivered),
                    TotalRevenue = orders.Sum(o => o.FinalPrice),
                    AverageOrderValue = orders.Average(o => o.FinalPrice),
                    TotalDiscount = orders.Sum(o => o.TotalPrice - o.FinalPrice),

                    // OrderDetail
                    TotalProductsSold = orders.SelectMany(o => o.OrderDetails).Sum(d => d.Quantity),

                    TopProducts = orders.SelectMany(o => o.OrderDetails)
                        .GroupBy(d => d.BoxTypeId)
                        .Select(g => new TopProductDTO
                        {
                            BoxTypeName = g.First().BoxType?.Name ?? "Unknown",
                            QuantitySold = g.Sum(x => x.Quantity),
                            Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                        })
                        .OrderByDescending(x => x.QuantitySold)
                        .Take(10)
                        .ToList(),

                    // Customer
                    TopCustomers = orders
                        .GroupBy(o => o.UserId)
                        .Select(g => new TopCustomerDTO
                        {
                            UserId = g.Key,
                            UserName = g.First().User?.FullName ?? "Unknown",
                            TotalSpent = g.Sum(x => x.FinalPrice),
                            OrderCount = g.Count()
                        })
                        .OrderByDescending(x => x.TotalSpent)
                        .Take(10)
                        .ToList(),

                    // Payment & Delivery
                    PaymentMethodStats = orders
                        .GroupBy(o => o.PaymentMethod.ToString())
                        .ToDictionary(g => g.Key, g => g.Count()),

                    DeliveryMethodStats = orders
                        .GroupBy(o => o.DeliveryMethod.ToString())
                        .ToDictionary(g => g.Key, g => g.Count()),

                    // Theo thời gian
                    RevenueByDate = orders
                        .GroupBy(o => o.CreatedAt.Date)
                        .Select(g => new RevenueByDateDTO
                        {
                            Date = g.Key,
                            Revenue = g.Sum(x => x.FinalPrice),
                            OrderCount = g.Count()
                        })
                        .OrderBy(x => x.Date)
                        .ToList()
                };

                return ApiResult<OrderStatisticsResponse>.Success(response, "Lấy thống kê đơn hàng thành công!");
            }
            catch (Exception ex)
            {
                return ApiResult<OrderStatisticsResponse>.Failure(
                    new Exception("Lỗi khi lấy thống kê đơn hàng: " + ex.Message));
            }
        }

    }
}
