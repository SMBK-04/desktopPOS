using System.Text;
using EnterprisePOS.Shared.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EnterprisePOS.ClientPOS.Services
{
    public class PrintService
    {
        public byte[] GenerateCashierReceipt(Sale sale, Tenant tenant)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"--- {tenant.CompanyName} ---");
            sb.AppendLine($"Branch: {tenant.BranchName}");
            sb.AppendLine($"Invoice: {sale.InvoiceNumber}");
            sb.AppendLine($"Date: {sale.Timestamp.ToLocalTime()}");
            sb.AppendLine(new string('-', 32));

            foreach (var item in sale.SaleItems)
            {
                sb.AppendLine($"{item.ProductName,-20} {item.Quantity,3} {item.SubTotal,7:C}");
            }

            sb.AppendLine(new string('-', 32));
            sb.AppendLine($"Tax: {sale.TaxAmount:C}");
            sb.AppendLine($"TOTAL: {sale.TotalAmount:C}");
            sb.AppendLine($"Paid by: {sale.PaymentMethod}");
            sb.AppendLine();
            sb.AppendLine("Thank you for your business!");
            sb.AppendLine(new string('\n', 4));

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        public byte[] GenerateKitchenReceipt(Sale sale)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== KITCHEN ORDER ===");
            sb.AppendLine($"Order: {sale.InvoiceNumber}");
            sb.AppendLine($"Type: {sale.OrderType}");
            sb.AppendLine($"Time: {sale.Timestamp.ToLocalTime():HH:mm:ss}");
            sb.AppendLine(new string('=', 32));

            foreach (var item in sale.SaleItems)
            {
                sb.AppendLine($"[ {item.Quantity} ] {item.ProductName}");
            }

            sb.AppendLine(new string('=', 32));
            sb.AppendLine(new string('\n', 4));

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        public void PrintToNetworkPrinter(byte[] data, string ipAddress)
        {
            // Stub for real implementation using Sockets:
            // using var client = new System.Net.Sockets.TcpClient(ipAddress, 9100);
            // using var stream = client.GetStream();
            // stream.Write(data, 0, data.Length);
        }

        public void PrintSilently(byte[] data, string? printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName) || data == null || data.Length == 0) return;

            try
            {
                // In a real WPF app, this would use RawPrinterHelper or PrintDocument.
                // For demonstration, we simulate the print job by saving the receipt to the desktop.
                string desktop = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
                string text = System.Text.Encoding.ASCII.GetString(data);
                // Sanitize printer name for filename
                string safePrinterName = string.Join("_", printerName.Split(System.IO.Path.GetInvalidFileNameChars()));
                string filename = $"Receipt_{safePrinterName}_{System.Guid.NewGuid().ToString("N").Substring(0, 6)}.txt";
                System.IO.File.WriteAllText(System.IO.Path.Combine(desktop, filename), text);
            }
            catch { }
        }

        public void KickCashDrawer()
        {
            // Typical ESC/POS drawer kick command
            byte[] kickCommand = new byte[] { 27, 112, 0, 25, 250 };
            // PrintToNetworkPrinter(kickCommand, "192.168.1.100");
        }

        public string GenerateEndOfDayPdfReport(Shift shift)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            string desktop = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            string filepath = System.IO.Path.Combine(desktop, $"DaySales_{shift.StartTime:yyyyMMdd}_{System.Guid.NewGuid().ToString("N").Substring(0, 6)}.pdf");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);

                    void ComposeHeader(IContainer container)
                    {
                        container.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text($"End of Day Report").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text($"Shift ID: {shift.Id}");
                                column.Item().Text($"Generated: {DateTime.Now:g}");
                            });
                        });
                    }

                    void ComposeContent(IContainer container)
                    {
                        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
                        {
                            column.Item().Text("Shift Summary").FontSize(14).SemiBold();
                            column.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            
                            column.Item().Text($"Start Time: {shift.StartTime.ToLocalTime():g}");
                            column.Item().Text($"End Time: {shift.EndTime?.ToLocalTime():g}");
                            column.Item().Text($"Cashier ID: {shift.CashierId}");
                            column.Item().PaddingTop(10);
                            
                            column.Item().Text("Financials").FontSize(14).SemiBold();
                            column.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            
                            column.Item().Text($"Opening Float: {shift.OpeningFloat:C}");
                            column.Item().Text($"Expected Cash: {shift.ExpectedCash:C}");
                            column.Item().Text($"Actual Closing Cash: {shift.ClosingCash:C}");
                            column.Item().Text($"Discrepancy: {shift.Discrepancy:C}").FontColor(shift.Discrepancy < 0 ? Colors.Red.Medium : Colors.Green.Medium);
                        });
                    }

                    void ComposeFooter(IContainer container)
                    {
                        container.AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    }
                });
            }).GeneratePdf(filepath);

            return filepath;
        }
    }
}
