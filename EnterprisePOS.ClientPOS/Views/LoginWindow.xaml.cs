using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Input;
using EnterprisePOS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ClientPOS.Views
{
    public partial class LoginWindow : Window
    {
        public bool IsAuthenticated { get; private set; }
        // We store email as a stand-in for the logged-in user identity
        public string LoggedInEmail { get; private set; } = string.Empty;
        // We create a synthetic LocalUser from cloud credentials
        public LocalUser LoggedInUser { get; private set; } = null!;

        private static readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5079") };

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new Data.LocalDbContext(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Data.LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
                await context.Database.MigrateAsync();
                
                var settings = await context.LocalSettings.FirstOrDefaultAsync();
                if (settings != null && !string.IsNullOrEmpty(settings.RememberMeToken))
                {
                    var user = await context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == settings.RememberMeToken);
                    if (user != null)
                    {
                        LoggedInEmail = user.Email;
                        LoggedInUser = user;
                        IsAuthenticated = true;
                        DialogResult = true;
                        Close();
                        return;
                    }
                }
            }
            catch { }
            
            txtEmail.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            Authenticate();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Authenticate();
        }

        private async void Authenticate()
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                txtError.Text = "Please enter both email and password.";
                return;
            }

            txtError.Text = "Signing in...";

            try
            {
                // Authenticate against the cloud API — same email/password as provisioned in Super Admin
                var dto = new { Email = email, Password = password };
                var response = await _httpClient.PostAsJsonAsync("/api/tenants/login", dto);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TenantLoginResult>();
                    if (result != null)
                    {
                        LoggedInEmail = email;
                        
                        using var context = new Data.LocalDbContext(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Data.LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
                        await context.Database.MigrateAsync();

                        var localUser = await context.Users.FirstOrDefaultAsync(u => u.Id == result.TenantId);
                        if (localUser == null)
                        {
                            localUser = new LocalUser
                            {
                                Id = result.TenantId,
                                TenantId = result.TenantId,
                                Email = email,
                                Role = Shared.Enums.Role.RestaurantAdmin,
                                PasswordHash = Shared.Services.PasswordHasher.HashPassword(password)
                            };
                            context.Users.Add(localUser);
                        }
                        else
                        {
                            localUser.Email = email;
                            localUser.TenantId = result.TenantId;
                            localUser.PasswordHash = Shared.Services.PasswordHasher.HashPassword(password);
                            context.Users.Update(localUser);
                        }
                        
                        var settings = await context.LocalSettings.FirstOrDefaultAsync();
                        if (settings == null)
                        {
                            settings = new LocalSettings();
                            context.LocalSettings.Add(settings);
                        }

                        if (chkRememberMe.IsChecked == true)
                        {
                            settings.RememberMeEmail = email;
                            settings.RememberMeToken = localUser.Id.ToString();
                        }
                        else
                        {
                            settings.RememberMeEmail = string.Empty;
                            settings.RememberMeToken = string.Empty;
                        }

                        await context.SaveChangesAsync();

                        LoggedInUser = localUser;
                        IsAuthenticated = true;
                        DialogResult = true;
                        Close();
                    }
                }
                else
                {
                    txtError.Text = "Invalid email or password.";
                    txtPassword.Clear();
                }
            }
            catch (Exception ex)
            {
                // Fallback: try local SQLite login if API is unavailable
                try 
                {
                    await TryLocalLogin(email, password);
                } 
                catch 
                {
                    txtError.Text = "API Error: " + ex.Message;
                }
            }
        }

        private async System.Threading.Tasks.Task TryLocalLogin(string email, string password)
        {
            try
            {
                using var context = new Data.LocalDbContext(
                    new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Data.LocalDbContext>()
                        .UseSqlite("Data Source=localpos.db").Options);
                await context.Database.MigrateAsync();

                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (user != null && Shared.Services.PasswordHasher.VerifyPassword(user.PasswordHash, password))
                {
                    LoggedInUser = user;
                    IsAuthenticated = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    txtError.Text = "Invalid email or password.";
                    txtPassword.Clear();
                }
            }
            catch (Exception ex)
            {
                txtError.Text = "Login error: " + ex.Message;
            }
        }

        private class TenantLoginResult
        {
            public Guid TenantId { get; set; }
        }
    }
}
