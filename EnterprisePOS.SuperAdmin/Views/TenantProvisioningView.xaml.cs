using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

namespace EnterprisePOS.SuperAdmin.Views
{
    public partial class TenantProvisioningView : UserControl
    {
        private static readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5079") };

        public TenantProvisioningView()
        {
            InitializeComponent();
        }

        private async void BtnProvision_Click(object sender, RoutedEventArgs e)
        {
            txtResult.Text = "Provisioning...";
            try
            {
                var dto = new
                {
                    CompanyName = txtCompany.Text,
                    Email = txtEmail.Text,
                    BranchName = txtBranch.Text,
                    AdminPassword = txtPassword.Password
                };

                var response = await _httpClient.PostAsJsonAsync("/api/tenants/provision", dto);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    txtResult.Text = "Success! " + result;
                    txtCompany.Clear(); txtEmail.Clear(); txtBranch.Clear(); txtPassword.Clear();
                }
                else
                {
                    txtResult.Text = "Error: " + response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                txtResult.Text = "Exception: " + ex.Message + "\n(The embedded API server may still be starting up. Please try again in a few seconds.)";
            }
        }
    }
}
