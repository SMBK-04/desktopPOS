using System;
using System.Collections.Generic;
using EnterprisePOS.Shared.Enums;

namespace EnterprisePOS.Shared.Models
{
    public class LocalSettings
    {
        public string Id { get; set; } = "Default"; // Single row
        public string EncryptedLicenseToken { get; set; } = string.Empty;
        public DateTime LastSyncTime { get; set; }
        public string CashierPrinterName { get; set; } = string.Empty;
        public string KitchenPrinterName { get; set; } = string.Empty;
        public bool IsPaymentDueBannerActive { get; set; }
        public string RememberMeEmail { get; set; } = string.Empty;
        public string RememberMeToken { get; set; } = string.Empty;
    }

    public class LocalUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public Role Role { get; set; }
    }

    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#333333";
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public bool IsOutOfStock { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public string Barcode { get; set; } = string.Empty;
        
        public Category? Category { get; set; }
    }

    public class Shift
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid CashierId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal OpeningFloat { get; set; }
        public decimal? ClosingCash { get; set; }
        public decimal? ExpectedCash { get; set; }
        public decimal? Discrepancy { get; set; }
        public bool IsClosed { get; set; }
        
        public LocalUser? Cashier { get; set; }
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }

    public class Sale
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid ShiftId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public OrderType OrderType { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public bool IsSynced { get; set; }
        
        public Shift? Shift { get; set; }
        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }

    public class SaleItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SaleId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }
        
        public Sale? Sale { get; set; }
        public Product? Product { get; set; }
    }
}
