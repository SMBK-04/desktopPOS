using System;
using System.Linq;
using System.Windows.Controls;
using EnterprisePOS.ClientPOS.Data;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ClientPOS.Views
{
    public partial class DashboardView : UserControl
    {
        private Guid _tenantId;
        
        public DashboardView(Guid tenantId)
        {
            InitializeComponent();
            _tenantId = tenantId;
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            txtDate.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");

            using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
            
            var today = DateTime.UtcNow.Date;
            var sales = await context.Sales.Where(s => s.TenantId == _tenantId && s.Timestamp >= today).ToListAsync();

            // Stat Cards
            txtOverallSales.Text  = $"Rs {sales.Sum(s => s.TotalAmount):N2}";
            txtTotalOrders.Text   = sales.Count.ToString();
            txtTotalProducts.Text = (await context.Products.CountAsync(p => p.TenantId == _tenantId)).ToString();
            txtTotalUsers.Text    = (await context.Users.CountAsync(u => u.TenantId == _tenantId)).ToString();

            // Recent Sales (last 10)
            var recent = await context.Sales
                .Where(s => s.TenantId == _tenantId)
                .OrderByDescending(s => s.Timestamp)
                .Take(10)
                .ToListAsync();
            dgRecentSales.ItemsSource = recent;
        }
    }
}
