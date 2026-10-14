using EnterprisePOS.Models.ViewModels;
using EnterprisePOS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterprisePOS.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService) => _reportService = reportService;

        public IActionResult Index() => View(new ReportFilterViewModel());

        [HttpPost]
        public async Task<IActionResult> ProfitLoss(ReportFilterViewModel filter)
        {
            var report = await _reportService.GetProfitLossAsync(filter.FromDate, filter.ToDate);
            return View(report);
        }

        [HttpPost]
        public async Task<IActionResult> TopProducts(ReportFilterViewModel filter)
        {
            var products = await _reportService.GetTopProductsAsync(filter.FromDate, filter.ToDate);
            ViewBag.From = filter.FromDate;
            ViewBag.To = filter.ToDate;
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> DailySales(ReportFilterViewModel filter)
        {
            var daily = await _reportService.GetDailySalesAsync(filter.FromDate, filter.ToDate);
            ViewBag.From = filter.FromDate;
            ViewBag.To = filter.ToDate;
            return View(daily);
        }

        public async Task<IActionResult> ExportCsv(DateTime from, DateTime to)
        {
            var bytes = await _reportService.ExportSalesToCsvAsync(from, to);
            return File(bytes, "text/csv", $"Sales_{from:yyyyMMdd}_{to:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> ExportPdf(DateTime from, DateTime to)
        {
            var bytes = await _reportService.ExportSalesToPdfAsync(from, to);
            return File(bytes, "application/pdf", $"SalesReport_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf");
        }
    }
}
