using System.Windows;

namespace EnterprisePOS.SuperAdmin.Views
{
    public partial class InputDialog : Window
    {
        public string ResponseText => txtResponse.Text;

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
            txtPrompt.Text = prompt;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
