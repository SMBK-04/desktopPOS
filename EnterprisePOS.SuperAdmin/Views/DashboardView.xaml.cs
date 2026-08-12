using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using EnterprisePOS.Shared.Models;

namespace EnterprisePOS.SuperAdmin.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5079") };

        public DashboardView()
        {
            InitializeComponent();
            Loaded += DashboardView_Loaded;
        }

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var tenants = await _httpClient.GetFromJsonAsync<Tenant[]>("/api/tenants");
                if (tenants != null)
                {
                    int activeTenants = tenants.Length;
                    decimal mrr = activeTenants * 1000.00m;

                    txtTenants.Text = activeTenants.ToString();
                    txtMrr.Text = "Rs " + mrr.ToString("N0");
                }
            }
            catch (Exception)
            {
                // API might not be running
                txtTenants.Text = "API Error";
            }
        }
    }
}
