using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace EnterprisePOS.SuperAdmin.Views
{
    public partial class SettingsView : UserControl
    {
        private static readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5079") };

        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tenants");
                txtApiStatus.Text = response.IsSuccessStatusCode ? "✅ Running on port 5079" : "⚠️ Error: " + response.StatusCode;
            }
            catch
            {
                txtApiStatus.Text = "❌ Not Running";
                txtApiStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Settings saved.\nMonthly Rate: Rs {txtMonthlyRate.Text}\nDefault POS Credentials:\n  Email: {txtDefaultEmail.Text}\n  Password: {txtDefaultPassword.Text}",
                "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
