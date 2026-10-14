using EnterprisePOS.Models.Entities;

namespace EnterprisePOS.Models.ViewModels
{
    /// <summary>Dashboard KPI and chart data sent to the dashboard view.</summary>
    public class DashboardStatsViewModel
    {
        // Shift Status
        public bool IsShiftActive { get; set; }
        public DateTime? ShiftStart { get; set; }

        // Store Operations & Delivery Controls
        public bool IsOnlineOrderingEnabled { get; set; } = true;
        public bool IsDeliveryEnabled { get; set; } = true;
        public bool IsTemporarilyClosed { get; set; } = false;
        public string StoreName { get; set; } = string.Empty;

        // KPI Cards
        public decimal TodaysRevenue { get; set; }
        public decimal TodaysProfit { get; set; }
        public int TodaysOrders { get; set; }
        public decimal TodaysExpenses { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }

        // Weekly revenue for chart (7 days)
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> RevenueData { get; set; } = new();
        public List<decimal> ProfitData { get; set; } = new();

        // Top 5 products by qty sold today
        public List<TopProductViewModel> TopProducts { get; set; } = new();

        // Recent sales table
        public List<Sale> RecentSales { get; set; } = new();

        // Low stock alerts
        public List<Product> LowStockProducts { get; set; } = new();
    }

    public class TopProductViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DailySalesViewModel
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
    }

    public class ProfitLossViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal GrossProfit => TotalRevenue - TotalCost;
        public decimal NetProfit => GrossProfit - TotalExpenses;
        public decimal ProfitMarginPercent => TotalRevenue > 0 ? Math.Round((NetProfit / TotalRevenue) * 100, 2) : 0;
        public int TotalOrders { get; set; }
    }
}
