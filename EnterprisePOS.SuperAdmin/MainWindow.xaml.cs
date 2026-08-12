using System.Windows;
using EnterprisePOS.SuperAdmin.Views;

namespace EnterprisePOS.SuperAdmin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainContent.Content = new DashboardView();
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DashboardView();
        }

        private void BtnTenants_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new TenantProvisioningView();
        }

        private void BtnSubscriptions_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SubscriptionBillingView();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SettingsView();
        }
    }
}