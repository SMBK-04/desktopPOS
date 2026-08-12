using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using EnterprisePOS.ClientPOS.Data;
using EnterprisePOS.ClientPOS.Services;
using EnterprisePOS.ClientPOS.ViewModels;
using EnterprisePOS.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EnterprisePOS.ClientPOS
{
    public class PosDisplayItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Emoji { get; set; } = "🍔";
        public string? CategoryName { get; set; }
        public bool IsDeal { get; set; }
        public bool IsOutOfStock { get; set; }
        public string? Barcode { get; set; }
    }

    public partial class MainWindow : Window
    {
        private IHost _host = null!;
        private DispatcherTimer _timer = null!;
        private Guid _activeShiftId;
        private LocalUser _loggedInUser = null!;
        private ObservableCollection<CartItemViewModel> _cartItems = new ObservableCollection<CartItemViewModel>();
        
        private List<Product> _allProducts = new List<Product>();
        private List<LocalDeal> _allDeals = new List<LocalDeal>();
        private List<PosDisplayItem> _displayItems = new List<PosDisplayItem>();
        private string _currentCategory = "All";
        private string _currentSearch = "";

        public MainWindow()
        {
            InitializeComponent();
            icCart.ItemsSource = _cartItems;
            _cartItems.CollectionChanged += (s, e) => CalculateTotals();
            
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHostedService<BackgroundSyncService>();
                    services.AddSingleton<PrintService>();
                })
                .Build();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();

            var loginWindow = new Views.LoginWindow();
            if (loginWindow.ShowDialog() == true && loginWindow.IsAuthenticated)
            {
                _loggedInUser = loginWindow.LoggedInUser;
                ApplyRolePermissions(_loggedInUser);
                this.Show();
                
                await _host.StartAsync();

                try
                {
                    // Ensure database is migrated once, sequentially, before background timer starts
                    using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
                    await context.Database.MigrateAsync();
                    
                    await LoadProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error initializing database: " + ex.Message);
                }

                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(10)
                };
                _timer.Tick += Timer_Tick;
                _timer.Start();
                
                Timer_Tick(null, null); 
            }
            else
            {
                this.Close();
            }
        }

        private async void BtnStartDay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
                var shift = new Shift
                {
                    TenantId = _loggedInUser.TenantId,
                    CashierId = _loggedInUser.Id,
                    StartTime = DateTime.UtcNow,
                    OpeningFloat = 0,
                    IsClosed = false
                };
                context.Shifts.Add(shift);
                await context.SaveChangesAsync();
                
                _activeShiftId = shift.Id;
                ShiftOverlay.Visibility = Visibility.Collapsed;
                btnStartDay.Visibility = Visibility.Collapsed;
                btnEndDay.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start shift: " + ex.Message);
            }
        }

        private void BtnEndDay_Click(object sender, RoutedEventArgs e)
        {
            var shiftWindow = new Views.ShiftCloseWindow(_activeShiftId);
            if (shiftWindow.ShowDialog() == true)
            {
                MessageBox.Show("Shift closed successfully. Application will now exit.");
                this.Close();
            }
        }

        private async System.Threading.Tasks.Task LoadProducts()
        {
            using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
            _allProducts = await context.Products.Include(p => p.Category).Where(p => p.TenantId == _loggedInUser.TenantId).ToListAsync();
            _allDeals = await context.Deals.Where(d => d.TenantId == _loggedInUser.TenantId).ToListAsync();
            
            _displayItems.Clear();
            foreach (var p in _allProducts)
            {
                _displayItems.Add(new PosDisplayItem { Id = p.Id, Name = p.Name, Price = p.Price, Emoji = "🍔", CategoryName = p.Category?.Name, IsDeal = false, IsOutOfStock = p.IsOutOfStock, Barcode = p.Barcode });
            }
            foreach (var d in _allDeals)
            {
                _displayItems.Add(new PosDisplayItem { Id = d.Id, Name = d.Name, Price = d.Price, Emoji = "🏷️", CategoryName = "Deals", IsDeal = true, IsOutOfStock = false });
            }

            var categories = _allProducts.Where(p => p.Category != null).Select(p => p.Category!.Name).Distinct().ToList();
            categories.Insert(0, "All");
            icCategories.ItemsSource = categories;

            FilterProducts();
        }

        private void DealsCategory_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            _currentCategory = "Deals";
            FilterProducts();
        }

        private void ShowProductGrid()
        {
            // No-op now since we unified the grid
        }

        private void FilterProducts()
        {
            var filtered = _displayItems.AsEnumerable();
            
            if (_currentCategory == "Deals")
                filtered = filtered.Where(p => p.IsDeal);
            else if (_currentCategory != "All")
                filtered = filtered.Where(p => !p.IsDeal && p.CategoryName == _currentCategory);
                
            if (!string.IsNullOrWhiteSpace(_currentSearch))
                filtered = filtered.Where(p => p.Name.Contains(_currentSearch, StringComparison.OrdinalIgnoreCase) || (!p.IsDeal && p.Barcode != null && p.Barcode.Contains(_currentSearch)));

            icProducts.ItemsSource = filtered.ToList();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearch = txtSearch.Text;
            FilterProducts();
        }

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            ShowProductGrid();
            if (sender is RadioButton rb && rb.Tag is string cat)
            {
                _currentCategory = cat;
                FilterProducts();
            }
        }

        private void Product_Click(object sender, MouseButtonEventArgs e)
        {
            if (_activeShiftId == Guid.Empty) return; // Safeguard
            
            if (sender is Border border && border.Tag is Guid itemId)
            {
                var item = _displayItems.FirstOrDefault(p => p.Id == itemId);
                if (item != null)
                {
                    var existingItem = _cartItems.FirstOrDefault(c => c.ProductId == itemId);
                    if (existingItem != null)
                    {
                        existingItem.Quantity++;
                    }
                    else
                    {
                        var vm = new CartItemViewModel
                        {
                            ProductId = item.Id,
                            ProductName = item.IsDeal ? $"🏷️ {item.Name}" : item.Name,
                            UnitPrice = item.Price,
                            Quantity = 1
                        };
                        vm.PropertyChanged += (s, ev) => CalculateTotals();
                        _cartItems.Add(vm);
                    }
                    CalculateTotals();
                }
            }
        }

        private void BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid productId)
            {
                var item = _cartItems.FirstOrDefault(c => c.ProductId == productId);
                if (item != null)
                {
                    item.Quantity--;
                    if (item.Quantity <= 0) _cartItems.Remove(item);
                }
            }
        }

        private void BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid productId)
            {
                var item = _cartItems.FirstOrDefault(c => c.ProductId == productId);
                if (item != null) item.Quantity++;
            }
        }

        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid productId)
            {
                var item = _cartItems.FirstOrDefault(c => c.ProductId == productId);
                if (item != null) _cartItems.Remove(item);
            }
        }

        private void BtnClearCart_Click(object sender, RoutedEventArgs e)
        {
            _cartItems.Clear();
        }

        private void CalculateTotals()
        {
            decimal total = _cartItems.Sum(c => c.SubTotal);
            txtGrandTotal.Text = total.ToString("C");
            txtCartCount.Text = $"{_cartItems.Sum(c => c.Quantity)} items";
            pnlEmptyCart.Visibility = _cartItems.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void BtnCheckoutCash_Click(object sender, RoutedEventArgs e)
        {
            if (!_cartItems.Any() || _activeShiftId == Guid.Empty) return;
            decimal total = _cartItems.Sum(c => c.SubTotal);
            await CompleteSaleAsync(total, 0, Shared.Enums.PaymentMethod.Cash); // Tax is simplified out for this replica
        }

        private async void BtnCheckoutCard_Click(object sender, RoutedEventArgs e)
        {
            // Replaced by Online Payment
        }

        private async void BtnCheckoutOnline_Click(object sender, RoutedEventArgs e)
        {
            if (!_cartItems.Any() || _activeShiftId == Guid.Empty) return;
            decimal total = _cartItems.Sum(c => c.SubTotal);
            await CompleteSaleAsync(total, 0, Shared.Enums.PaymentMethod.Card); // Treat online as Card/Non-Cash
        }

        private async System.Threading.Tasks.Task CompleteSaleAsync(decimal total, decimal tax, Shared.Enums.PaymentMethod method)
        {
            var sale = new Sale
            {
                TenantId = _loggedInUser.TenantId,
                ShiftId = _activeShiftId,
                TotalAmount = total,
                TaxAmount = tax,
                PaymentMethod = method,
                OrderType = Shared.Enums.OrderType.Takeaway,
                InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}",
                IsSynced = false
            };

            foreach (var item in _cartItems)
            {
                sale.SaleItems.Add(new SaleItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    SubTotal = item.SubTotal
                });
            }

            try
            {
                using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
                context.Sales.Add(sale);
                await context.SaveChangesAsync();
                
                var printService = _host.Services.GetRequiredService<PrintService>();
                var tenant = new Tenant { CompanyName = "EnterprisePOS", BranchName = "Main" };
                
                var cashierReceipt = printService.GenerateCashierReceipt(sale, tenant);
                var kitchenReceipt = printService.GenerateKitchenReceipt(sale);
                
                var settings = await context.LocalSettings.FirstOrDefaultAsync();
                
                // Silent printing
                printService.PrintSilently(cashierReceipt, settings?.CashierPrinterName);
                printService.PrintSilently(kitchenReceipt, settings?.KitchenPrinterName);
                printService.KickCashDrawer();
                
                MessageBox.Show($"Payment Complete! ({method})\nOrder processed and receipts printed silently.");
                _cartItems.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Checkout failed: " + ex.Message);
            }
        }

        private async void Timer_Tick(object? sender, EventArgs? e)
        {
            try
            {
                using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
                var settings = await context.LocalSettings.FirstOrDefaultAsync();
                if (settings != null)
                {
                    LicenseWarningBanner.Visibility = settings.IsPaymentDueBannerActive ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch
            {
                // Ignore transient db lock errors in background timer
            }
        }

        private void HideAllPanels()
        {
            POSPanel.Visibility = Visibility.Collapsed;
            AdminContent.Visibility = Visibility.Collapsed;
        }

        private void BtnNavDashboard_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            AdminContent.Content = new Views.DashboardView(_loggedInUser.TenantId);
            AdminContent.Visibility = Visibility.Visible;
        }

        private void BtnNavPOS_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            POSPanel.Visibility = Visibility.Visible;
        }

        private void BtnNavProducts_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            var menuManager = new Views.MenuManagerView(_loggedInUser.TenantId);
            menuManager.ProductsUpdated += async () => 
            {
                // Must marshal back to UI thread if we modified ObservableCollections
                await Application.Current.Dispatcher.InvokeAsync(async () => await LoadProducts());
            };
            AdminContent.Content = menuManager;
            AdminContent.Visibility = Visibility.Visible;
        }

        private void BtnNavDeals_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            var dealsManager = new Views.DealsManagerView(_loggedInUser.TenantId);
            dealsManager.DealsUpdated += async () => 
            {
                await Application.Current.Dispatcher.InvokeAsync(async () => await LoadProducts());
            };
            AdminContent.Content = dealsManager;
            AdminContent.Visibility = Visibility.Visible;
        }

        private void BtnNavSalesHistory_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            AdminContent.Content = new Views.SalesHistoryView(_loggedInUser.TenantId);
            AdminContent.Visibility = Visibility.Visible;
        }

        private void BtnNavUsers_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            AdminContent.Content = new Views.UsersManagerView(_loggedInUser.TenantId);
            AdminContent.Visibility = Visibility.Visible;
        }

        private void BtnNavSettings_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            AdminContent.Content = new Views.SettingsView();
            AdminContent.Visibility = Visibility.Visible;
        }

        private async void BtnSignOut_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Are you sure you want to sign out?", "Confirm Sign Out", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            if (_activeShiftId != Guid.Empty)
            {
                using var context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
                var shift = await context.Shifts.FindAsync(_activeShiftId);
                if (shift != null)
                {
                    try
                    {
                        var printService = new Services.PrintService();
                        string pdfPath = printService.GenerateEndOfDayPdfReport(shift);
                        MessageBox.Show($"End of day report saved to:\n{pdfPath}", "Report Generated", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch { } // Ignore PDF error on signout
                }
            }

            this.Hide();

            var login = new Views.LoginWindow();
            if (login.ShowDialog() == true && login.IsAuthenticated)
            {
                _loggedInUser = login.LoggedInUser;
                ApplyRolePermissions(_loggedInUser);
                _activeShiftId = Guid.Empty;
                ShiftOverlay.Visibility = Visibility.Visible;
                btnStartDay.Visibility = Visibility.Visible;
                btnEndDay.Visibility = Visibility.Collapsed;
                _cartItems.Clear();
                CalculateTotals();
                this.Show();
                await LoadProducts();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
        
        private void ApplyRolePermissions(LocalUser? user)
        {
            if (user == null) return;
            
            bool isAdmin = user.Role == Shared.Enums.Role.RestaurantAdmin || user.Role == Shared.Enums.Role.Manager;
            
            lblNavAdmin.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnNavDashboard.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnNavProducts.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnNavDeals.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnNavSalesHistory.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnNavUsers.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnNavSettings.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            
            // Go to POS by default
            BtnNavPOS_Click(this, new RoutedEventArgs());
        }

        private async void MainWindow_Closed(object? sender, EventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
