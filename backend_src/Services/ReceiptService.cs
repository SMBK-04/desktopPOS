using EnterprisePOS.Models.Entities;
using EnterprisePOS.Services.Interfaces;
using EnterprisePOS.Data;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using iText.Kernel.Geom;

namespace EnterprisePOS.Services
{
    /// <summary>
    /// Generates both Cashier and Kitchen PDF receipts using iText7.
    /// Cashier receipt: full pricing, totals, tax, payment info.
    /// Kitchen receipt: items + quantities ONLY — no prices.
    /// </summary>
    public class ReceiptService : IReceiptService
    {
        private readonly ApplicationDbContext _db;

        public ReceiptService(ApplicationDbContext db)
        {
            _db = db;
        }

        private static readonly Color PrimaryColor = new DeviceRgb(0, 0, 0);  // Black
        private static readonly Color DarkColor = new DeviceRgb(17, 24, 39);
        private static readonly Color GrayColor = new DeviceRgb(107, 114, 128);
        private static readonly Color KitchenAccent = new DeviceRgb(0, 0, 0); // Black

        public byte[] GenerateCashierReceipt(Sale sale)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf);

            var pageSize = new PageSize(226.77f, 1000f);
            pdf.SetDefaultPageSize(pageSize);
            doc.SetMargins(10, 10, 10, 10);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var blackColor = DeviceGray.BLACK;

            // Retrieve tenant details dynamically
            var tenant = _db.Tenants.IgnoreQueryFilters().FirstOrDefault(t => t.Id == sale.TenantId);
            string tenantName = tenant?.Name ?? "Breadline";
            string tenantBranch = tenant?.BranchName ?? "North Karachi";

            // ─── Header ─────────────────────────────────────────────────────
            doc.Add(new Paragraph(tenantName)
                .SetFont(boldFont).SetFontSize(14)
                .SetFontColor(blackColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(0));

            string headerText = tenantName.ToLower() == "breadline" 
                ? "House R-641 Babul Islam Town Sector 9\nNorth\nKarachi Pakistan\nTel No : 0371-1226804" 
                : $"{tenantBranch}";

            doc.Add(new Paragraph(headerText)
                .SetFont(normalFont).SetFontSize(9)
                .SetFontColor(blackColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // ─── Invoice Info ────────────────────────────────────────────────
            var infoPara = new Paragraph()
                .SetFont(normalFont).SetFontSize(9)
                .SetFontColor(blackColor)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetMarginBottom(10);
                
            infoPara.Add($"Date: {sale.CreatedAt.ToString("dd/MM/yyyy HH:mm")}\n");
            infoPara.Add($"Sale No/Ref: {sale.InvoiceNumber}\n");
            var cashierName = sale.Cashier?.FullName ?? "Administrator";
            infoPara.Add($"Sales Associate: {cashierName}\n");
            var customerName = sale.Customer?.Name ?? "Walk-in Customer";
            infoPara.Add($"Customer: {customerName}");
            
            doc.Add(infoPara);

            // ─── Items Table ─────────────────────────────────────────────────
            var itemTable = new Table(new float[] { 2, 1, 1, 1.5f }).UseAllAvailableWidth();
            itemTable.SetMarginBottom(5);
            
            iText.Layout.Borders.Border solidBorder = new iText.Layout.Borders.SolidBorder(blackColor, 1f);
            
            Cell MakeHeader(string text) {
                return new Cell().Add(new Paragraph(text).SetFont(boldFont).SetFontSize(9))
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetBorderTop(solidBorder)
                    .SetBorderBottom(solidBorder)
                    .SetTextAlignment(TextAlignment.LEFT);
            }
            Cell MakeHeaderRight(string text) {
                return new Cell().Add(new Paragraph(text).SetFont(boldFont).SetFontSize(9))
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetBorderTop(solidBorder)
                    .SetBorderBottom(solidBorder)
                    .SetTextAlignment(TextAlignment.RIGHT);
            }
            
            itemTable.AddHeaderCell(MakeHeader("Name"));
            itemTable.AddHeaderCell(MakeHeaderRight("Price"));
            itemTable.AddHeaderCell(MakeHeaderRight("Qty"));
            itemTable.AddHeaderCell(MakeHeaderRight("Amount"));

            int idx = 1;
            foreach (var item in sale.SaleItems)
            {
                string itemName = $"#{idx} {item.ProductName}";
                if (!string.IsNullOrEmpty(item.Modifiers)) itemName += $" ({item.Modifiers})";
                
                var nameCell = new Cell().Add(new Paragraph(itemName).SetFont(normalFont).SetFontSize(9))
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.LEFT);
                itemTable.AddCell(nameCell);
                
                itemTable.AddCell(new Cell().Add(new Paragraph(item.UnitPrice.ToString("F2")).SetFont(normalFont).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT));
                itemTable.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString("F2")).SetFont(normalFont).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT));
                itemTable.AddCell(new Cell().Add(new Paragraph(item.LineTotal.ToString("F2")).SetFont(normalFont).SetFontSize(9)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT));
                
                idx++;
            }

            doc.Add(itemTable);
            doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.DottedLine(1f, 2f)).SetMarginBottom(5));

            // ─── Totals ──────────────────────────────────────────────────────
            var totalsTable = new Table(new float[] { 1, 1 }).UseAllAvailableWidth();
            
            Cell MakeTotalCell(string text, bool right, bool bold = false) {
                var p = new Paragraph(text).SetFont(bold ? boldFont : normalFont).SetFontSize(9);
                if (right) p.SetTextAlignment(TextAlignment.RIGHT);
                return new Cell().Add(p).SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            }
            
            totalsTable.AddCell(MakeTotalCell("Total", false, true));
            totalsTable.AddCell(MakeTotalCell(sale.TotalAmount.ToString("F2"), true, true));
            
            totalsTable.AddCell(MakeTotalCell("Grand total", false, true));
            totalsTable.AddCell(MakeTotalCell(sale.TotalAmount.ToString("F2"), true, true));

            doc.Add(totalsTable);
            doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(1f)).SetMarginTop(2).SetMarginBottom(2));

            var paidTable = new Table(new float[] { 1, 1 }).UseAllAvailableWidth();
            paidTable.AddCell(MakeTotalCell("Paid Amount", false, true));
            paidTable.AddCell(MakeTotalCell(sale.AmountPaid.ToString("F2"), true, true));
            doc.Add(paidTable);
            
            doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(1f)).SetMarginTop(2).SetMarginBottom(5));

            var changeTable = new Table(new float[] { 1, 1 }).UseAllAvailableWidth();
            string pm = sale.PaymentMethod.ToString().ToLower();
            changeTable.AddCell(MakeTotalCell($"Paid by: {pm}", false, false));
            changeTable.AddCell(MakeTotalCell($"Change: {sale.ChangeGiven.ToString("F2")}", true, false));
            doc.Add(changeTable);

            // ─── Footer ──────────────────────────────────────────────────────
            doc.Add(new Paragraph("\nThank you for shopping with us. Please\ncome again\n")
                .SetFont(boldFont).SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(blackColor));
                
            doc.Add(new Paragraph("Powered by Enterprise POS 03161036764")
                .SetFont(normalFont).SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(blackColor));

            // ─── Trim Page Height ────────────────────────────────────────────
            var renderer = (iText.Layout.Renderer.DocumentRenderer)doc.GetRenderer();
            float currentY = renderer.GetCurrentArea().GetBBox().GetTop();
            float bottom = currentY - 10f; // 10 points bottom margin
            pdf.GetPage(1).SetMediaBox(new iText.Kernel.Geom.Rectangle(0, bottom, 226.77f, 1000f - bottom));
            var action = iText.Kernel.Pdf.Action.PdfAction.CreateJavaScript("this.print({bUI: false, bSilent: true, bShrinkToFit: true});");
            pdf.GetCatalog().SetOpenAction(action);

            doc.Close();
            return ms.ToArray();
        }

        public byte[] GenerateKitchenReceipt(Sale sale)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf);

            var pageSize = new PageSize(226.77f, 1000f);
            pdf.SetDefaultPageSize(pageSize);
            doc.SetMargins(10, 10, 10, 10);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // ─── Kitchen Header ──────────────────────────────────────────────
            doc.Add(new Paragraph("** KITCHEN ORDER **")
                .SetFont(boldFont).SetFontSize(16)
                .SetFontColor(KitchenAccent)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine()));

            // ─── Order Info ──────────────────────────────────────────────────
            doc.Add(new Paragraph($"Order: {sale.InvoiceNumber}")
                .SetFont(boldFont).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"Type: {sale.OrderType}")
                .SetFont(boldFont).SetFontSize(12)
                .SetFontColor(KitchenAccent)
                .SetTextAlignment(TextAlignment.CENTER));

            if (!string.IsNullOrEmpty(sale.TableNumber))
                doc.Add(new Paragraph($"Table: {sale.TableNumber}")
                    .SetFont(boldFont).SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER));

            if (!string.IsNullOrEmpty(sale.OrderToken))
                doc.Add(new Paragraph($"Token: #{sale.OrderToken}")
                    .SetFont(boldFont).SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph(sale.CreatedAt.ToString("HH:mm dd/MM/yyyy"))
                .SetFont(normalFont).SetFontSize(10)
                .SetFontColor(GrayColor)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine()));

            // ─── Items (NO PRICES) ───────────────────────────────────────────
            foreach (var item in sale.SaleItems)
            {
                var itemPara = new Paragraph($"x{item.Quantity}  {item.ProductName}")
                    .SetFont(boldFont).SetFontSize(13);
                doc.Add(itemPara);

                if (!string.IsNullOrEmpty(item.Modifiers))
                    doc.Add(new Paragraph($"   >> {item.Modifiers}")
                        .SetFont(normalFont).SetFontSize(11)
                        .SetFontColor(GrayColor));
            }

            if (!string.IsNullOrEmpty(sale.Notes))
            {
                doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.DottedLine()));
                doc.Add(new Paragraph($"NOTE: {sale.Notes}")
                    .SetFont(boldFont).SetFontSize(11)
                    .SetFontColor(new DeviceRgb(239, 68, 68)));
            }

            doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine()));
            doc.Add(new Paragraph("END OF ORDER")
                .SetFont(boldFont).SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER));

            // ─── Trim Page Height ────────────────────────────────────────────
            var renderer = (iText.Layout.Renderer.DocumentRenderer)doc.GetRenderer();
            float currentY = renderer.GetCurrentArea().GetBBox().GetTop();
            float bottom = currentY - 10f; // 10 points bottom margin
            pdf.GetPage(1).SetMediaBox(new iText.Kernel.Geom.Rectangle(0, bottom, 226.77f, 1000f - bottom));

            doc.Close();
            return ms.ToArray();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────
        private static Cell Cell(string text, PdfFont font, TextAlignment align = TextAlignment.LEFT)
            => new Cell().Add(new Paragraph(text).SetFont(font).SetFontSize(10))
                         .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                         .SetTextAlignment(align)
                         .SetPaddingTop(2).SetPaddingBottom(2);

        private static Cell HeaderCell(string text, PdfFont font)
            => new Cell().Add(new Paragraph(text).SetFont(font).SetFontSize(10))
                         .SetBackgroundColor(new DeviceRgb(243, 244, 246))
                         .SetTextAlignment(TextAlignment.CENTER);

        private static void AddTotalRow(Table table, string label, string value, PdfFont font)
        {
            table.AddCell(new Cell().Add(new Paragraph(label).SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Cell().Add(new Paragraph(value).SetFont(font).SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT));
        }
    }
}
