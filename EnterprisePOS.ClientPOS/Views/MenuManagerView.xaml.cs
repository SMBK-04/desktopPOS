using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnterprisePOS.ClientPOS.Data;
using EnterprisePOS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ClientPOS.Views
{
    public partial class MenuManagerView : UserControl
    {
        public event System.Action? ProductsUpdated;
        
        private readonly LocalDbContext _context;
        private Product? _editingProduct;
        private Guid _tenantId;

        public MenuManagerView(Guid tenantId)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
            Loaded += MenuManagerView_Loaded;
        }

        private async void MenuManagerView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            await _context.Database.MigrateAsync(); // Ensure DB is there for testing
            await _context.Categories.Include(c => c.Products).Where(c => c.TenantId == _tenantId).LoadAsync();
            lstCategories.ItemsSource = _context.Categories.Local.ToObservableCollection();
        }

        private async void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCategoryName.Text)) return;

            var category = new Category { TenantId = _tenantId, Name = txtCategoryName.Text };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            txtCategoryName.Clear();
        }

        private async void BtnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Category category)
            {
                var result = MessageBox.Show($"Delete category '{category.Name}' and all its products?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _context.Categories.Remove(category);
                    await _context.SaveChangesAsync();
                    icProductCards.ItemsSource = null;
                    txtProductsHeader.Text = "Select a category";
                    ProductsUpdated?.Invoke();
                }
            }
        }

        private void LstCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstCategories.SelectedItem is Category category)
            {
                txtProductsHeader.Text = $"Products in: {category.Name}";
                icProductCards.ItemsSource = category.Products.ToList();
            }
            else
            {
                icProductCards.ItemsSource = null;
                txtProductsHeader.Text = "Select a category";
            }
        }

        private void BtnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Product product)
            {
                _editingProduct = product;
                txtProductName.Text = product.Name;
                txtProductPrice.Text = product.Price.ToString("0.00");
                txtProductCost.Text = product.CostPrice.ToString("0.00");
                btnAddProduct.Content = "Update Product";
                btnCancelEdit.Visibility = Visibility.Visible;
            }
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            _editingProduct = null;
            txtProductName.Clear();
            txtProductPrice.Clear();
            txtProductCost.Clear();
            btnAddProduct.Content = "Add Product";
            btnCancelEdit.Visibility = Visibility.Collapsed;
        }

        private async void BtnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Product product)
            {
                var result = MessageBox.Show($"Delete product '{product.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                    ProductsUpdated?.Invoke();
                    LstCategories_SelectionChanged(lstCategories, null!);
                }
            }
        }

        private async void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is Category category)
            {
                if (decimal.TryParse(txtProductPrice.Text, out var price) && decimal.TryParse(txtProductCost.Text, out var cost))
                {
                    if (_editingProduct != null)
                    {
                        _editingProduct.Name = txtProductName.Text;
                        _editingProduct.Price = price;
                        _editingProduct.CostPrice = cost;
                        _context.Products.Update(_editingProduct);
                        
                        _editingProduct = null;
                        btnAddProduct.Content = "Add Product";
                        btnCancelEdit.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        var product = new Product
                        {
                            TenantId = _tenantId,
                            Name = txtProductName.Text,
                            Price = price,
                            CostPrice = cost,
                            CategoryId = category.Id
                        };
                        _context.Products.Add(product);
                    }
                    
                    await _context.SaveChangesAsync();
                    ProductsUpdated?.Invoke();
                    
                    txtProductName.Clear(); txtProductPrice.Clear(); txtProductCost.Clear();
                    
                    // Refresh card grid
                    LstCategories_SelectionChanged(lstCategories, null!);
                }
            }
            else
            {
                MessageBox.Show("Please select a category first.", "No Category Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is Product product)
            {
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                ProductsUpdated?.Invoke();
            }
        }
    }
}
