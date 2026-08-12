using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using EnterprisePOS.Shared.Models;

namespace EnterprisePOS.SuperAdmin.Views
{
    public partial class SubscriptionBillingView : UserControl
    {
        private static readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5079") };

        public SubscriptionBillingView()
        {
            InitializeComponent();
            Loaded += SubscriptionBillingView_Loaded;
        }

        private async void SubscriptionBillingView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTenants();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadTenants();
        }

        private async System.Threading.Tasks.Task LoadTenants()
        {
            try
            {
                var tenants = await _httpClient.GetFromJsonAsync<List<Tenant>>("/api/tenants");
                dgTenants.ItemsSource = tenants;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load tenants: " + ex.Message);
            }
        }

        private async void BtnMarkDue_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Guid tenantId)
            {
                await _httpClient.PostAsync($"/api/tenants/{tenantId}/mark-due", null);
                await LoadTenants();
            }
        }

        private async void BtnMarkPaid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Guid tenantId)
            {
                await _httpClient.PostAsync($"/api/tenants/{tenantId}/mark-paid", null);
                await LoadTenants();
            }
        }

        private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Guid tenantId)
            {
                var inputDialog = new InputDialog("Enter New Password", "New Password:");
                if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.ResponseText))
                {
                    try
                    {
                        var dto = new { NewPassword = inputDialog.ResponseText };
                        var response = await _httpClient.PostAsJsonAsync($"/api/tenants/{tenantId}/change-password", dto);
                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Password changed successfully.");
                            await LoadTenants();
                        }
                        else
                        {
                            MessageBox.Show("Failed to change password: " + response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Guid tenantId)
            {
                var result = MessageBox.Show("Are you sure you want to delete this tenant? This action cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"/api/tenants/{tenantId}");
                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Tenant deleted successfully.");
                            await LoadTenants();
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete tenant: " + response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
            }
        }
    }
}
