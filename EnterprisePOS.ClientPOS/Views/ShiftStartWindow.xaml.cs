using System;
using System.Windows;
using EnterprisePOS.ClientPOS.Data;
using EnterprisePOS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ClientPOS.Views
{
    public partial class ShiftStartWindow : Window
    {
        public Guid ActiveShiftId { get; private set; }
        private Guid _cashierId;

        public ShiftStartWindow(Guid cashierId)
        {
            InitializeComponent();
            _cashierId = cashierId;
            txtFloat.Focus();
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtFloat.Text, out decimal openingFloat))
            {
                try
                {
                    using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
                    await context.Database.MigrateAsync();

                    var shift = new Shift
                    {
                        StartTime = DateTime.UtcNow,
                        OpeningFloat = openingFloat,
                        CashierId = _cashierId 
                    };

                    context.Shifts.Add(shift);
                    await context.SaveChangesAsync();

                    ActiveShiftId = shift.Id;
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    txtError.Text = "DB Error: " + ex.Message;
                }
            }
            else
            {
                txtError.Text = "Invalid amount.";
            }
        }
    }
}
