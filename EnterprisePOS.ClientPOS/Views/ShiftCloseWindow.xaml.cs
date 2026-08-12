using System;
using System.Linq;
using System.Windows;
using EnterprisePOS.ClientPOS.Data;
using EnterprisePOS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ClientPOS.Views
{
    public partial class ShiftCloseWindow : Window
    {
        private readonly Guid _shiftId;
        private decimal _expectedCash;
        private Shift _shift = null!;

        public ShiftCloseWindow(Guid shiftId)
        {
            InitializeComponent();
            _shiftId = shiftId;
            Loaded += ShiftCloseWindow_Loaded;
        }

        private async void ShiftCloseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
                _shift = await context.Shifts.Include(s => s.Sales).FirstOrDefaultAsync(s => s.Id == _shiftId);
                
                if (_shift == null)
                {
                    MessageBox.Show("Shift not found!");
                    Close();
                    return;
                }

                decimal cashSales = _shift.Sales.Where(s => s.PaymentMethod == Shared.Enums.PaymentMethod.Cash).Sum(s => s.TotalAmount);
                _expectedCash = _shift.OpeningFloat + cashSales;

                txtOpeningFloat.Text = _shift.OpeningFloat.ToString("C");
                txtCashSales.Text = cashSales.ToString("C");
                txtExpectedCash.Text = _expectedCash.ToString("C");
            }
            catch (Exception ex)
            {
                txtError.Text = ex.Message;
            }
        }

        private void TxtActualCash_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtActualCash.Text, out decimal actualCash))
            {
                decimal discrepancy = actualCash - _expectedCash;
                txtDiscrepancy.Text = discrepancy.ToString("C");
                txtDiscrepancy.Foreground = discrepancy < 0 ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtActualCash.Text, out decimal actualCash))
            {
                try
                {
                    using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
                    var shift = await context.Shifts.FindAsync(_shiftId);
                    if (shift != null)
                    {
                        shift.EndTime = DateTime.UtcNow;
                        shift.ClosingCash = actualCash;
                        shift.ExpectedCash = _expectedCash;
                        shift.Discrepancy = actualCash - _expectedCash;
                        shift.IsClosed = true;

                        await context.SaveChangesAsync();

                        // Print EOD PDF
                        try
                        {
                            var printService = new Services.PrintService();
                            string pdfPath = printService.GenerateEndOfDayPdfReport(shift);
                            MessageBox.Show($"End of day report saved to:\n{pdfPath}", "Report Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception printEx)
                        {
                            MessageBox.Show("Shift closed, but failed to generate PDF: " + printEx.Message, "PDF Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        DialogResult = true;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    txtError.Text = "DB Error: " + ex.Message;
                }
            }
            else
            {
                txtError.Text = "Please enter valid actual cash.";
            }
        }
    }
}
