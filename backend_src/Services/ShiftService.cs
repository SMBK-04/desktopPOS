using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EnterprisePOS.Services
{
    public class ShiftService : IShiftService
    {
        private readonly ApplicationDbContext _db;

        public ShiftService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<WorkShift?> GetActiveShiftAsync()
        {
            return await _db.WorkShifts
                .Where(s => !s.IsClosed)
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefaultAsync();
        }

        public async Task<WorkShift> StartDayAsync(string userId)
        {
            var active = await GetActiveShiftAsync();
            if (active != null) return active; // Already started

            var shift = new WorkShift
            {
                StartTime = TimeHelper.PKT,
                IsClosed = false,
                OpenedByUserId = userId,
                TotalSales = 0,
                TotalProfit = 0
            };

            _db.WorkShifts.Add(shift);
            await _db.SaveChangesAsync();
            return shift;
        }

        public async Task<WorkShift> EndDayAsync()
        {
            var shift = await GetActiveShiftAsync();
            if (shift == null) throw new System.Exception("No active shift found.");

            shift.EndTime = TimeHelper.PKT;
            shift.IsClosed = true;

            // Calculate total sales and profit for this shift
            var sales = await _db.Sales
                .Where(s => s.Status == SaleStatus.Completed && s.CreatedAt >= shift.StartTime && s.CreatedAt <= shift.EndTime)
                .ToListAsync();

            shift.TotalSales = sales.Sum(s => s.TotalAmount);
            
            // Calculate Profit = TotalAmount - (CostPrice of all items)
            var saleIds = sales.Select(s => s.Id).ToList();
            var saleItems = await _db.SaleItems
                .Include(si => si.Product)
                .Where(si => saleIds.Contains(si.SaleId))
                .ToListAsync();

            decimal totalCost = saleItems.Sum(si => (si.Product?.CostPrice ?? 0) * si.Quantity);
            shift.TotalProfit = shift.TotalSales - totalCost;

            await _db.SaveChangesAsync();
            return shift;
        }
    }
}
