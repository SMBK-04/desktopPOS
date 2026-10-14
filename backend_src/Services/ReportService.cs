using CsvHelper;
using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;
using EnterprisePOS.Services.Interfaces;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace EnterprisePOS.Services
{
    /// <summary>
    /// Generates business reports: Profit/Loss, daily sales, top products.
    /// Supports export to CSV and PDF.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db) => _db = db;

        public async Task<ProfitLossViewModel> GetProfitLossAsync(DateTime from, DateTime to)
        {
            var toEnd = to.AddDays(1);

            var sales = await _db.Sales
                .Include(s => s.SaleItems).ThenInclude(si => si.Product)
                .Where(s => s.CreatedAt >= from && s.CreatedAt < toEnd && s.Status == SaleStatus.Completed)
                .ToListAsync();

            var expenseAmounts = await _db.Expenses
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < toEnd)
                .Select(e => e.Amount)
                .ToListAsync();
            var expenses = expenseAmounts.Sum();

            var revenue = sales.Sum(s => s.TotalAmount);
            var cost = sales.SelectMany(s => s.SaleItems).Sum(si => (si.Product != null ? si.Product.CostPrice : 0) * si.Quantity);

            return new ProfitLossViewModel
            {
                From = from,
                To = to,
                TotalRevenue = revenue,
                TotalCost = cost,
                TotalExpenses = expenses,
                TotalOrders = sales.Count
            };
        }

        public async Task<IEnumerable<TopProductViewModel>> GetTopProductsAsync(DateTime from, DateTime to, int top = 10)
        {
            var toEnd = to.AddDays(1);
            var items = await _db.SaleItems
                .Include(si => si.Sale)
                .Where(si => si.Sale.CreatedAt >= from && si.Sale.CreatedAt < toEnd && si.Sale.Status == SaleStatus.Completed)
                .Select(si => new { si.ProductName, si.Quantity, si.LineTotal })
                .ToListAsync();

            return items
                .GroupBy(si => si.ProductName)
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(top)
                .ToList();
        }

        public async Task<IEnumerable<DailySalesViewModel>> GetDailySalesAsync(DateTime from, DateTime to)
        {
            var toEnd = to.AddDays(1);
            var sales = await _db.Sales
                .Include(s => s.SaleItems).ThenInclude(si => si.Product)
                .Where(s => s.CreatedAt >= from && s.CreatedAt < toEnd && s.Status == SaleStatus.Completed)
                .ToListAsync();

            return sales
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new DailySalesViewModel
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(s => s.TotalAmount),
                    Profit = g.Sum(s => s.TotalAmount) - g.SelectMany(s => s.SaleItems).Sum(si => (si.Product != null ? si.Product.CostPrice : 0) * si.Quantity)
                })
                .OrderBy(d => d.Date);
        }

        public async Task<byte[]> ExportSalesToCsvAsync(DateTime from, DateTime to)
        {
            var sales = (await GetSalesWithItemsAsync(from, to)).ToList();

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            var records = sales.Select(s => new
            {
                s.InvoiceNumber,
                Date = s.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                s.OrderType,
                Customer = s.Customer?.Name ?? "Walk-In",
                s.SubTotal,
                s.DiscountAmount,
                s.TaxAmount,
                s.TotalAmount,
                s.PaymentMethod,
                s.Status
            });

            csv.WriteRecords(records);
            await writer.FlushAsync();
            return ms.ToArray();
        }

        public async Task<byte[]> ExportSalesToPdfAsync(DateTime from, DateTime to)
        {
            var sales = (await GetSalesWithItemsAsync(from, to)).ToList();

            var tenantId = _db.CurrentTenantId;
            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
            string reportTitle = !string.IsNullOrWhiteSpace(tenant?.Name) 
                ? $"{tenant.Name.ToUpper()} - SALES REPORT" 
                : "Sales Report";

            using var ms = new MemoryStream();
            using var pdfWriter = new PdfWriter(ms);
            using var pdf = new PdfDocument(pdfWriter);
            using var doc = new Document(pdf);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var primaryColor = new DeviceRgb(99, 102, 241);

            doc.Add(new Paragraph(reportTitle)
                .SetFont(boldFont).SetFontSize(20)
                .SetFontColor(primaryColor)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"Period: {from:dd MMM yyyy} – {to:dd MMM yyyy}")
                .SetFont(normalFont).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine()));

            var table = new Table(new float[] { 2, 1.5f, 1, 1, 1, 1 }).UseAllAvailableWidth();
            foreach (var h in new[] { "Invoice", "Date", "Customer", "Revenue", "Payment", "Status" })
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(boldFont).SetFontSize(9))
                    .SetBackgroundColor(new DeviceRgb(243, 244, 246)));

            foreach (var s in sales)
            {
                table.AddCell(new Cell().Add(new Paragraph(s.InvoiceNumber).SetFont(normalFont).SetFontSize(9)).SetBorder(null));
                table.AddCell(new Cell().Add(new Paragraph(s.CreatedAt.ToString("dd/MM HH:mm")).SetFont(normalFont).SetFontSize(9)).SetBorder(null));
                table.AddCell(new Cell().Add(new Paragraph(s.Customer?.Name ?? "Walk-In").SetFont(normalFont).SetFontSize(9)).SetBorder(null));
                table.AddCell(new Cell().Add(new Paragraph(s.TotalAmount.ToString("F2")).SetFont(normalFont).SetFontSize(9)).SetBorder(null).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Cell().Add(new Paragraph(s.PaymentMethod.ToString()).SetFont(normalFont).SetFontSize(9)).SetBorder(null));
                table.AddCell(new Cell().Add(new Paragraph(s.Status.ToString()).SetFont(normalFont).SetFontSize(9)).SetBorder(null));
            }

            doc.Add(table);

            var totalRevenue = sales.Sum(s => s.TotalAmount);
            doc.Add(new Paragraph($"\nTotal Revenue: {totalRevenue:C2} | Total Orders: {sales.Count}")
                .SetFont(boldFont).SetFontSize(12));

            doc.Close();
            return ms.ToArray();
        }

        private async Task<IEnumerable<Sale>> GetSalesWithItemsAsync(DateTime from, DateTime to)
        {
            var toEnd = to.AddDays(1);
            return await _db.Sales
                .Include(s => s.SaleItems)
                .Include(s => s.Customer)
                .Include(s => s.Cashier)
                .Where(s => s.CreatedAt >= from && s.CreatedAt < toEnd && s.Status == SaleStatus.Completed)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<byte[]> GenerateDayEndPdfAsync(int shiftId)
        {
            var shift = await _db.WorkShifts.FindAsync(shiftId);
            if (shift == null) throw new Exception("Shift not found");

            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == shift.TenantId);
            string businessName = !string.IsNullOrWhiteSpace(tenant?.Name) ? tenant.Name : "Enterprise";
            string branchName = tenant?.BranchName ?? "";

            var sales = await _db.Sales
                .Include(s => s.SaleItems)
                .Where(s => s.CreatedAt >= shift.StartTime && s.CreatedAt <= shift.EndTime && s.Status == SaleStatus.Completed)
                .ToListAsync();

            var productStats = sales.SelectMany(s => s.SaleItems)
                .GroupBy(si => si.ProductName)
                .Select(g => new { 
                    ProductName = g.Key, 
                    Qty = g.Sum(x => x.Quantity), 
                    Total = g.Sum(x => x.LineTotal) 
                })
                .OrderByDescending(x => x.Qty)
                .ToList();

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var accentColor = new DeviceRgb(59, 130, 246); // Blue
            var grayColor = new DeviceRgb(243, 244, 246);
            
            // Header
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 100 })).UseAllAvailableWidth();
            headerTable.AddCell(new Cell().Add(new Paragraph($"{businessName.ToUpper()} POS - SHIFT REPORT")
                .SetFont(boldFont).SetFontSize(24).SetFontColor(accentColor))
                .SetBorder(null).SetTextAlignment(TextAlignment.CENTER));

            if (!string.IsNullOrWhiteSpace(branchName))
            {
                headerTable.AddCell(new Cell().Add(new Paragraph(branchName)
                    .SetFont(normalFont).SetFontSize(12).SetFontColor(ColorConstants.GRAY))
                    .SetBorder(null).SetTextAlignment(TextAlignment.CENTER));
            }

            doc.Add(headerTable);
            doc.Add(new Paragraph("\n"));

            // Shift Details
            doc.Add(new Paragraph($"Shift Started: {shift.StartTime:MMM dd, yyyy hh:mm tt}").SetFont(normalFont).SetFontSize(12));
            doc.Add(new Paragraph($"Shift Ended: {(shift.EndTime.HasValue ? shift.EndTime.Value.ToString("MMM dd, yyyy hh:mm tt") : "Ongoing")}").SetFont(normalFont).SetFontSize(12));
            doc.Add(new Paragraph("\n"));

            // Summary Cards
            var summaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
            summaryTable.AddCell(new Cell().Add(new Paragraph("Total Sales").SetFont(normalFont).SetFontSize(14).SetFontColor(ColorConstants.GRAY))
                .Add(new Paragraph($"Rs {shift.TotalSales:N2}").SetFont(boldFont).SetFontSize(20))
                .SetBackgroundColor(grayColor).SetPadding(15).SetBorder(null));
            summaryTable.AddCell(new Cell().Add(new Paragraph("Total Profit").SetFont(normalFont).SetFontSize(14).SetFontColor(ColorConstants.GRAY))
                .Add(new Paragraph($"Rs {shift.TotalProfit:N2}").SetFont(boldFont).SetFontSize(20))
                .SetBackgroundColor(grayColor).SetPadding(15).SetBorder(null));
            doc.Add(summaryTable);
            
            doc.Add(new Paragraph("\n\n"));
            doc.Add(new Paragraph("PRODUCTS SOLD").SetFont(boldFont).SetFontSize(16).SetFontColor(accentColor));
            doc.Add(new Paragraph("\n"));

            // Products Table
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 50, 25, 25 })).UseAllAvailableWidth();
            table.AddHeaderCell(new Cell().Add(new Paragraph("Product").SetFont(boldFont)).SetBackgroundColor(grayColor).SetBorder(null).SetPadding(8));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Quantity").SetFont(boldFont)).SetBackgroundColor(grayColor).SetBorder(null).SetPadding(8).SetTextAlignment(TextAlignment.CENTER));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Revenue").SetFont(boldFont)).SetBackgroundColor(grayColor).SetBorder(null).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT));
            
            foreach(var p in productStats)
            {
                table.AddCell(new Cell().Add(new Paragraph(p.ProductName).SetFont(normalFont)).SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.LIGHT_GRAY, 1)).SetBorderTop(null).SetBorderLeft(null).SetBorderRight(null).SetPadding(8));
                table.AddCell(new Cell().Add(new Paragraph(p.Qty.ToString()).SetFont(normalFont)).SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.LIGHT_GRAY, 1)).SetBorderTop(null).SetBorderLeft(null).SetBorderRight(null).SetPadding(8).SetTextAlignment(TextAlignment.CENTER));
                table.AddCell(new Cell().Add(new Paragraph($"Rs {p.Total:N2}").SetFont(normalFont)).SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.LIGHT_GRAY, 1)).SetBorderTop(null).SetBorderLeft(null).SetBorderRight(null).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT));
            }

            doc.Add(table);
            doc.Close();

            return ms.ToArray();
        }
    }
}
