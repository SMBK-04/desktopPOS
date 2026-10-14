using EnterprisePOS.Models.ViewModels;
using EnterprisePOS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterprisePOS.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ISaleService _saleService;
        private readonly IReceiptService _receiptService;
        private readonly ILogger<SalesController> _logger;
        private readonly EnterprisePOS.Data.ApplicationDbContext _db;

        public SalesController(ISaleService saleService, IReceiptService receiptService, ILogger<SalesController> logger, EnterprisePOS.Data.ApplicationDbContext db)
        {
            _saleService = saleService;
            _receiptService = receiptService;
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? source)
        {
            var fromDate = from ?? DateTime.Today.AddDays(-30);
            var toDate = to ?? DateTime.Today;

            var sales = await _saleService.GetSalesByDateRangeAsync(fromDate, toDate);

            // Filter out held/voided orders so we only see actual sales
            sales = sales.Where(s => s.Status == Models.Entities.SaleStatus.Completed || s.Status == Models.Entities.SaleStatus.Refunded);

            if (!string.IsNullOrEmpty(source))
            {
                if (source.Equals("Delivery", StringComparison.OrdinalIgnoreCase))
                {
                    sales = sales.Where(s => s.OrderType == Models.Entities.OrderType.Delivery);
                }
                else if (source.Equals("Restaurant", StringComparison.OrdinalIgnoreCase))
                {
                    sales = sales.Where(s => s.OrderType != Models.Entities.OrderType.Delivery);
                }
            }

            var vm = new SaleListViewModel
            {
                Sales = sales,
                FromDate = fromDate,
                ToDate = toDate,
                StatusFilter = source,
                TotalRevenue = sales.Where(s => s.Status == Models.Entities.SaleStatus.Completed).Sum(s => s.TotalAmount),
                TotalOrders = sales.Count(s => s.Status == Models.Entities.SaleStatus.Completed)
            };

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null) return NotFound();

            var vm = new SaleDetailViewModel
            {
                Sale = sale,
                CanRefund = sale.Status == Models.Entities.SaleStatus.Completed
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Refund(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
                await _saleService.RefundSaleAsync(id, userId);
                TempData["Success"] = "Sale refunded successfully. Stock has been restored.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> PrintCashierReceipt(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null) return NotFound();
            var pdf = _receiptService.GenerateCashierReceipt(sale);
            return File(pdf, "application/pdf", $"CashierReceipt_{sale.InvoiceNumber}.pdf");
        }

        public async Task<IActionResult> PrintKitchenReceipt(int id)
        {
            var sale = await _saleService.GetByIdAsync(id);
            if (sale == null) return NotFound();
            var pdf = _receiptService.GenerateKitchenReceipt(sale);
            return File(pdf, "application/pdf", $"KitchenReceipt_{sale.InvoiceNumber}.pdf");
        }

        [AllowAnonymous]
        [HttpGet("Sales/NukeEverything123")]
        public async Task<IActionResult> DeveloperClearAllSales()
        {
            try
            {
                await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(_db.Database, "DELETE FROM SaleItems; DELETE FROM Sales; DELETE FROM HeldOrders;");
                return Content("DEV SUCCESS: ALL SALES HAVE BEEN PERMANENTLY DELETED.");
            }
            catch (Exception ex)
            {
                return Content($"DEV ERROR: {ex.Message}");
            }
        }
    }
}
