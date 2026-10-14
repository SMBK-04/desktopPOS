using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace EnterprisePOS.Controllers
{
    [Authorize]
    public class ShiftController : Controller
    {
        private readonly IShiftService _shiftService;
        private readonly IReportService _reportService;
        private readonly ApplicationDbContext _db;

        public ShiftController(
            IShiftService shiftService,
            IReportService reportService,
            ApplicationDbContext db)
        {
            _shiftService = shiftService;
            _reportService = reportService;
            _db = db;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StartDay()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _shiftService.StartDayAsync(userId);
                TempData["Success"] = "Day started successfully. Sales are now unlocked.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EndDay()
        {
            try
            {
                var shift = await _shiftService.EndDayAsync();
                TempData["Success"] = $"Day ended successfully. Total Sales: {shift.TotalSales:C}, Profit: {shift.TotalProfit:C}";
                TempData["DownloadReportUrl"] = Url.Action("DayEndReport", new { id = shift.Id });
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DayEndReport(int id)
        {
            try
            {
                var shift = await _db.WorkShifts.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
                if (shift == null) return NotFound();

                var pdfBytes = await _reportService.GenerateDayEndPdfAsync(id);
                var startDateStr = shift.StartTime.ToString("yyyy-MM-dd");
                return File(pdfBytes, "application/pdf", $"DayEndReport_{startDateStr}.pdf");
            }
            catch
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
    }
}
