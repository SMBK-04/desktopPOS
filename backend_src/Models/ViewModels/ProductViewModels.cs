using System.ComponentModel.DataAnnotations;
using EnterprisePOS.Models.Entities;

namespace EnterprisePOS.Models.ViewModels
{
    // ─── Product Create ──────────────────────────────────────────────────────
    public class ProductCreateViewModel
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ProductCode { get; set; }

        [MaxLength(50)]
        public string? Barcode { get; set; }

        [MaxLength(50)]
        public string? SKU { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public ProductType ProductType { get; set; } = ProductType.Standard;

        [Required, Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal SellingPrice { get; set; }

        [Range(0, 100)]
        public decimal TaxRate { get; set; }

        public int CurrentStock { get; set; }
        public int LowStockThreshold { get; set; } = 5;
        public int ReorderLevel { get; set; } = 10;

        [Required]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool TrackInventory { get; set; } = false;

        public IFormFile? ImageFile { get; set; }

        // For dropdowns in view
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    }

    // ─── Product Edit ────────────────────────────────────────────────────────
    public class ProductEditViewModel : ProductCreateViewModel
    {
        public int Id { get; set; }
        public string? ExistingImagePath { get; set; }
    }

    // ─── Product List Item ───────────────────────────────────────────────────
    public class ProductListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProductCode { get; set; }
        public string? Barcode { get; set; }
        public string? SKU { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int CurrentStock { get; set; }
        public int LowStockThreshold { get; set; }
        public bool IsActive { get; set; }
        public string? ImagePath { get; set; }
        public bool IsLowStock => CurrentStock > 0 && CurrentStock <= LowStockThreshold;
        public bool IsOutOfStock => CurrentStock <= 0;
    }

    // ─── POS Product Card ────────────────────────────────────────────────────
    public class PosProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal TaxRate { get; set; }
        public string? Barcode { get; set; }
        public string? ImagePath { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int Stock { get; set; }
    }
}
