using System.Windows;
using System.Windows.Controls;
using EnterprisePOS.ClientPOS.Data;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ClientPOS.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
            var settings = await context.LocalSettings.FirstOrDefaultAsync();
            if (settings != null)
            {
                txtCustomerPrinter.Text = settings.CashierPrinterName;
                txtKitchenPrinter.Text = settings.KitchenPrinterName;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
            var settings = await context.LocalSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new Shared.Models.LocalSettings();
                context.LocalSettings.Add(settings);
            }
            
            settings.CashierPrinterName = txtCustomerPrinter.Text.Trim();
            settings.KitchenPrinterName = txtKitchenPrinter.Text.Trim();
            
            await context.SaveChangesAsync();
            
            txtStatus.Text = "Settings saved successfully!";
        }
    }
}
