using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnterprisePOS.ClientPOS.Data;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ClientPOS.Views
{
    public partial class SalesHistoryView : UserControl
    {
        private Guid _tenantId;
        
        public SalesHistoryView(Guid tenantId)
        {
            InitializeComponent();
            _tenantId = tenantId;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
            
            var sales = await context.Sales.Where(s => s.TenantId == _tenantId).OrderByDescending(s => s.Timestamp).Take(100).ToListAsync();
            dgSales.ItemsSource = sales;
        }
    }
}
