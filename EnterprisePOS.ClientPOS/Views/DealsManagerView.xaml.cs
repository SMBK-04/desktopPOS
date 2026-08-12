using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnterprisePOS.ClientPOS.Data;
using EnterprisePOS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ClientPOS.Views
{
    // Helper ViewModel for product checkboxes in the deal form
    public class ProductCheckItem : System.ComponentModel.INotifyPropertyChanged
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        private bool _isSelected;
        public bool IsSelected 
        { 
            get => _isSelected; 
            set { _isSelected = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); }
        }
        
        private int _quantity = 1;
        public int Quantity 
        { 
            get => _quantity; 
            set { _quantity = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Quantity))); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class DealsManagerView : UserControl
    {
        public event System.Action? DealsUpdated;
        
        private LocalDbContext _context;
        private LocalDeal? _editingDeal;
        private List<ProductCheckItem> _productCheckItems = new();
        private Guid _tenantId;

        public DealsManagerView(Guid tenantId)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _context = new LocalDbContext(new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options);
            Loaded += DealsManagerView_Loaded;
        }

        private async void DealsManagerView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            // Load all products for checklist
            var products = await _context.Products.Where(p => p.TenantId == _tenantId).OrderBy(p => p.Name).ToListAsync();
            _productCheckItems = products.Select(p => new ProductCheckItem { Id = p.Id, Name = p.Name, IsSelected = false, Quantity = 1 }).ToList();
            icProductsChecklist.ItemsSource = _productCheckItems;

            // Load all deals
            var deals = await _context.Deals.Include(d => d.DealProducts).Where(d => d.TenantId == _tenantId).ToListAsync();
            icDeals.ItemsSource = deals;
        }

        private void BtnCancelDeal_Click(object sender, RoutedEventArgs e)
        {
            _editingDeal = null;
            txtFormTitle.Text = "New Deal";
            txtDealName.Clear();
            txtDealPrice.Clear();
            btnCancelDeal.Visibility = Visibility.Collapsed;
            foreach (var item in _productCheckItems) 
            {
                item.IsSelected = false;
                item.Quantity = 1;
            }
            icProductsChecklist.Items.Refresh();
        }

        private async void BtnSaveDeal_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDealName.Text) || !decimal.TryParse(txtDealPrice.Text, out var price))
            {
                MessageBox.Show("Please enter a valid deal name and price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedProducts = _productCheckItems.Where(p => p.IsSelected).ToList();

            if (_editingDeal != null)
            {
                // Update existing deal
                _editingDeal.Name = txtDealName.Text;
                _editingDeal.Price = price;

                // Remove old product links and re-add
                var oldLinks = _context.DealProducts.Where(dp => dp.DealId == _editingDeal.Id);
                _context.DealProducts.RemoveRange(oldLinks);

                foreach (var pItem in selectedProducts)
                    _context.DealProducts.Add(new LocalDealProduct { DealId = _editingDeal.Id, ProductId = pItem.Id, Quantity = pItem.Quantity });

                _context.Deals.Update(_editingDeal);
            }
            else
            {
                // Create new deal
                var deal = new LocalDeal { TenantId = _tenantId, Name = txtDealName.Text, Price = price };
                _context.Deals.Add(deal);
                await _context.SaveChangesAsync(); // Save first to get the Id

                foreach (var pItem in selectedProducts)
                    _context.DealProducts.Add(new LocalDealProduct { DealId = deal.Id, ProductId = pItem.Id, Quantity = pItem.Quantity });
            }

            await _context.SaveChangesAsync();
            DealsUpdated?.Invoke();
            
            BtnCancelDeal_Click(null!, null!);
            await LoadData();
        }

        private async void BtnEditDeal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is LocalDeal deal)
            {
                _editingDeal = deal;
                txtFormTitle.Text = "Edit Deal";
                txtDealName.Text = deal.Name;
                txtDealPrice.Text = deal.Price.ToString("0.00");
                btnCancelDeal.Visibility = Visibility.Visible;

                // Load linked products
                var linkedProducts = await _context.DealProducts.Where(dp => dp.DealId == deal.Id).ToListAsync();
                foreach (var item in _productCheckItems)
                {
                    var link = linkedProducts.FirstOrDefault(lp => lp.ProductId == item.Id);
                    if (link != null)
                    {
                        item.IsSelected = true;
                        item.Quantity = link.Quantity;
                    }
                    else
                    {
                        item.IsSelected = false;
                        item.Quantity = 1;
                    }
                }
                icProductsChecklist.Items.Refresh();
            }
        }

        private async void BtnDeleteDeal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is LocalDeal deal)
            {
                var result = MessageBox.Show($"Delete deal '{deal.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _context.Deals.Remove(deal);
                    await _context.SaveChangesAsync();
                    DealsUpdated?.Invoke();
                    await LoadData();
                }
            }
        }
    }
}
