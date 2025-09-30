using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class OrderStatisticsRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class OrderStatisticsResponse
    {
        // Tổng quan
        public int TotalOrders { get; set; }
        public int PaidOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public double TotalRevenue { get; set; }
        public double AverageOrderValue { get; set; }
        public double TotalDiscount { get; set; }

        // OrderDetail
        public int TotalProductsSold { get; set; }
        public List<TopProductDTO> TopProducts { get; set; } = new();

        // Customer
        public List<TopCustomerDTO> TopCustomers { get; set; } = new();

        // Payment & Delivery
        public Dictionary<string, int> PaymentMethodStats { get; set; } = new();
        public Dictionary<string, int> DeliveryMethodStats { get; set; } = new();

        // Theo thời gian
        public List<RevenueByDateDTO> RevenueByDate { get; set; } = new();
    }

    public class TopProductDTO
    {
        public string BoxTypeName { get; set; }
        public int QuantitySold { get; set; }
        public double Revenue { get; set; }
    }

    public class TopCustomerDTO
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }  // nếu join với bảng User
        public double TotalSpent { get; set; }
        public int OrderCount { get; set; }
    }

    public class RevenueByDateDTO
    {
        public DateTime Date { get; set; }
        public double Revenue { get; set; }
        public int OrderCount { get; set; }
    }

}
