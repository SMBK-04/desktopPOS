using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;
using EnterprisePOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EnterprisePOS.Services
{
    /// <summary>
    /// Core sales processing service: checkout, holds, refunds, and dashboard stats.
    /// </summary>
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventory;

        public SaleService(ApplicationDbContext db, IInventoryService inventory)
        {
            _db = db;
            _inventory = inventory;
        }

        public async Task<Sale?> GetByIdAsync(int id)
        {
            return await _db.Sales
                .Include(s => s.SaleItems).ThenInclude(si => si.Product)
                .Include(s => s.Customer)
                .Include(s => s.Cashier)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Sale>> GetTodaysSalesAsync()
        {
            var today = DateTime.Today;
            return await _db.Sales
                .Include(s => s.Customer)
                .Include(s => s.Cashier)
                .Where(s => s.CreatedAt >= today && s.Status == SaleStatus.Completed)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime from, DateTime to)
        {
            var toEnd = to.AddDays(1);
            return await _db.Sales
                .Include(s => s.SaleItems)
                .Include(s => s.Customer)
                .Include(s => s.Cashier)
                .Where(s => s.CreatedAt >= from && s.CreatedAt < toEnd)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Processes a full checkout: creates sale, reduces stock, saves to DB.
        /// </summary>
        public async Task<Sale> ProcessCheckoutAsync(CheckoutViewModel model, string cashierId)
        {
            // Calculate totals
            decimal subTotal = model.Items.Sum(i => i.UnitPrice * i.Quantity);
            decimal discountAmount = model.DiscountAmount > 0
                ? model.DiscountAmount
                : Math.Round(subTotal * (model.DiscountPercent / 100), 4);
            decimal taxableAmount = subTotal - discountAmount;
            decimal taxAmount = 0; // Tax removed
            decimal total = taxableAmount + taxAmount + model.ServiceCharge;

            var sale = new Sale
            {
                InvoiceNumber = GenerateInvoiceNumber(),
                Status = SaleStatus.Completed,
                OrderType = model.OrderType,
                PaymentMethod = model.PaymentMethod,
                TableNumber = model.TableNumber,
                OrderToken = model.OrderToken ?? Guid.NewGuid().ToString("N")[..8].ToUpper(),
                SubTotal = subTotal,
                DiscountAmount = discountAmount,
                DiscountPercent = model.DiscountPercent,
                TaxAmount = taxAmount,
                ServiceCharge = model.ServiceCharge,
                TotalAmount = total,
                AmountPaid = model.AmountPaid,
                ChangeGiven = Math.Max(0, model.AmountPaid - total),
                CustomerId = model.CustomerId,
                CashierId = cashierId,
                Notes = model.Notes,
                CreatedAt = TimeHelper.PKT,
                CompletedAt = TimeHelper.PKT,
                SaleItems = model.Items.Select(i => new SaleItem
                {
                    ProductId = i.ProductId,
                    DealId = i.DealId,
                    ProductName = i.ProductName,
                    Modifiers = i.Modifiers,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountPercent = i.DiscountPercent,
                    DiscountAmount = i.DiscountAmount,
                    TaxRate = 0,
                    TaxAmount = 0,
                    LineTotal = i.LineTotal
                }).ToList()
            };

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();

            // Reduce stock for each item bypassed (unlimited resources)
            /*
            foreach (var item in model.Items)
            {
                if (item.ProductId.HasValue)
                {
                    await _inventory.AdjustStockAsync(
                        item.ProductId.Value,
                        -item.Quantity,
                        InventoryMovementType.Sale,
                        $"Sale #{sale.InvoiceNumber}",
                        cashierId);
                }
                else if (item.DealId.HasValue)
                {
                    var dealProds = await _db.DealProducts.Where(dp => dp.DealId == item.DealId.Value).ToListAsync();
                    foreach (var dp in dealProds)
                    {
                        await _inventory.AdjustStockAsync(
                            dp.ProductId,
                            -item.Quantity,
                            InventoryMovementType.Sale,
                            $"Sale #{sale.InvoiceNumber} (Deal: {item.ProductName})",
                            cashierId);
                    }
                }
            }
            */

            return sale;
        }

        public async Task<Sale> RefundSaleAsync(int saleId, string userId)
        {
            var sale = await GetByIdAsync(saleId)
                ?? throw new KeyNotFoundException($"Sale {saleId} not found.");

            if (sale.Status == SaleStatus.Refunded)
                throw new InvalidOperationException("Sale already refunded.");

            sale.Status = SaleStatus.Refunded;
            await _db.SaveChangesAsync();

            // Restore inventory bypassed (unlimited resources)
            /*
            foreach (var item in sale.SaleItems)
            {
                if (item.ProductId.HasValue)
                {
                    await _inventory.AdjustStockAsync(
                        item.ProductId.Value,
                        item.Quantity,
                        InventoryMovementType.Return,
                        $"Refund for #{sale.InvoiceNumber}",
                        userId);
                }
                else if (item.DealId.HasValue)
                {
                    var dealProds = await _db.DealProducts.Where(dp => dp.DealId == item.DealId.Value).ToListAsync();
                    foreach (var dp in dealProds)
                    {
                        await _inventory.AdjustStockAsync(
                            dp.ProductId,
                            item.Quantity,
                            InventoryMovementType.Return,
                            $"Refund for #{sale.InvoiceNumber} (Deal: {item.ProductName})",
                            userId);
                    }
                }
            }
            */

            return sale;
        }

        public async Task HoldOrderAsync(HoldOrderViewModel model, string cashierId)
        {
            var holdRef = $"HOLD-{TimeHelper.PKT:yyyyMMddHHmmss}";
            var held = new HeldOrder
            {
                HoldReference = holdRef,
                TableNumber = model.TableNumber,
                CustomerName = model.CustomerName,
                CartJson = JsonSerializer.Serialize(model.Items, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                CashierId = cashierId,
                CreatedAt = TimeHelper.PKT
            };

            _db.HeldOrders.Add(held);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<HeldOrder>> GetHeldOrdersAsync()
        {
            return await _db.HeldOrders
                .Include(h => h.Cashier)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }

        public async Task<HeldOrder?> GetHeldOrderAsync(int id)
            => await _db.HeldOrders.FindAsync(id);

        public async Task DeleteHeldOrderAsync(int id)
        {
            var held = await _db.HeldOrders.FindAsync(id);
            if (held != null)
            {
                _db.HeldOrders.Remove(held);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<DashboardStatsViewModel> GetDashboardStatsAsync()
        {
            var activeShift = await _db.WorkShifts.Where(s => !s.IsClosed).OrderByDescending(s => s.StartTime).FirstOrDefaultAsync();
            var shiftStart = activeShift?.StartTime ?? TimeHelper.PKT.Date;
            var todayEnd = TimeHelper.PKT.Date.AddDays(1); // Future boundary for expenses if shift not closed

            var todaySales = await _db.Sales
                .Include(s => s.SaleItems).ThenInclude(si => si.Product)
                .Where(s => s.CreatedAt >= shiftStart && s.Status == SaleStatus.Completed)
                .ToListAsync();

            var todayRevenue = todaySales.Sum(s => s.TotalAmount);
            var todayCost = todaySales
                .SelectMany(s => s.SaleItems)
                .Sum(si => (si.Product != null ? si.Product.CostPrice : 0) * si.Quantity);
            var expenseAmounts = await _db.Expenses
                .Where(e => e.ExpenseDate >= shiftStart.Date && e.ExpenseDate < todayEnd)
                .Select(e => e.Amount)
                .ToListAsync();
            var todayExpenses = expenseAmounts.Sum();

            var last7 = TimeHelper.PKT.Date.AddDays(-6);

            // 7-day chart
            var weekSales = await _db.Sales
                .Include(s => s.SaleItems).ThenInclude(si => si.Product)
                .Where(s => s.CreatedAt >= last7 && s.Status == SaleStatus.Completed)
                .ToListAsync();

            var today = TimeHelper.PKT.Date;
            var labels = new List<string>();
            var revenues = new List<decimal>();
            var profits = new List<decimal>();
            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                // Compare by Date exactly
                var daySales = weekSales.Where(s => s.CreatedAt.Date == day);
                labels.Add(day.ToString("ddd"));
                revenues.Add(daySales.Sum(s => s.TotalAmount));
                var dayCost = daySales.SelectMany(s => s.SaleItems).Sum(si => (si.Product != null ? si.Product.CostPrice : 0) * si.Quantity);
                profits.Add(daySales.Sum(s => s.TotalAmount) - dayCost);
            }

            var shiftSaleItems = await _db.SaleItems
                .Include(si => si.Sale)
                .Where(si => si.Sale.CreatedAt >= shiftStart && si.Sale.Status == SaleStatus.Completed)
                .Select(si => new { si.ProductName, si.Quantity, si.LineTotal })
                .ToListAsync();

            var topProducts = shiftSaleItems
                .GroupBy(si => si.ProductName)
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToList();

            var recentSales = await _db.Sales
                .Include(s => s.Customer)
                .Where(s => s.Status == SaleStatus.Completed)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            var lowStockProducts = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.TrackInventory && p.CurrentStock <= p.LowStockThreshold)
                .OrderBy(p => p.CurrentStock)
                .ToListAsync();

            var outOfStockProducts = lowStockProducts.Where(p => p.CurrentStock <= 0).ToList();

            var vm = new DashboardStatsViewModel
            {
                IsShiftActive = activeShift != null,
                ShiftStart = activeShift?.StartTime,
                TodaysRevenue = todayRevenue,
                TodaysProfit = todayRevenue - todayCost,
                TodaysOrders = todaySales.Count,
                TodaysExpenses = todayExpenses,
                LowStockCount = lowStockProducts.Count(p => p.CurrentStock > 0),
                OutOfStockCount = outOfStockProducts.Count,
                ChartLabels = labels,
                RevenueData = revenues,
                ProfitData = profits,
                TopProducts = topProducts,
                RecentSales = todaySales.OrderByDescending(s => s.CreatedAt).Take(10).ToList(),
                LowStockProducts = lowStockProducts.Take(5).ToList()
            };

            return vm;
        }

        public string GenerateInvoiceNumber()
        {
            var date = TimeHelper.PKT.ToString("yyyyMMdd");
            var seq = (_db.Sales.IgnoreQueryFilters().Count() + 1).ToString("D4");
            return $"INV-{date}-{seq}";
        }
    }
}
