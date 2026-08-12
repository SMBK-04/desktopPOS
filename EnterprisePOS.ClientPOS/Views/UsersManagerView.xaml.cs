using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnterprisePOS.ClientPOS.Data;
using EnterprisePOS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ClientPOS.Views
{
    public partial class UsersManagerView : UserControl
    {
        private Guid _tenantId;
        private string _domain = "@restaurant.com";

        public UsersManagerView(Guid tenantId)
        {
            InitializeComponent();
            _tenantId = tenantId;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
            
            // Get domain from the current admin
            var admin = await context.Users.FirstOrDefaultAsync(u => u.Id == _tenantId);
            if (admin != null && admin.Email.Contains("@"))
            {
                _domain = "@" + admin.Email.Split('@').Last();
                txtDomain.Text = _domain;
            }

            var users = await context.Users.Where(u => u.TenantId == _tenantId).ToListAsync();
            dgUsers.ItemsSource = users;
        }

        private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsernamePrefix.Text.Trim();
            string password = txtPassword.Password;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter username and password.");
                return;
            }

            string fullEmail = username + _domain;
            Shared.Enums.Role role = cmbRole.SelectedIndex == 1 ? Shared.Enums.Role.Manager : Shared.Enums.Role.Cashier;

            using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
            
            if (await context.Users.AnyAsync(u => u.Email.ToLower() == fullEmail.ToLower()))
            {
                MessageBox.Show("User already exists!");
                return;
            }

            var user = new LocalUser
            {
                TenantId = _tenantId,
                Email = fullEmail,
                Role = role,
                PasswordHash = Shared.Services.PasswordHasher.HashPassword(password)
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            txtUsernamePrefix.Clear();
            txtPassword.Clear();
            await LoadDataAsync();
            MessageBox.Show("User created successfully!");
        }
    }
}
