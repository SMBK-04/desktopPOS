using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterprisePOS.Models.Entities
{
    /// <summary>
    /// Product types supported by the POS system.
    /// </summary>
    public enum ProductType
    {
        Standard = 0,
        Combo = 1,
        Service = 2
    }

    /// <summary>
    /// Core product entity representing sellable items in the POS.
    /// </summary>
    public class Product : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

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

        [Column(TypeName = "decimal(18,4)")]
        public decimal CostPrice { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SellingPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 0;       // Percentage e.g. 15.00

        public int CurrentStock { get; set; } = 0;
        public int LowStockThreshold { get; set; } = 5;
        public int ReorderLevel { get; set; } = 10;

        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;
        public bool TrackInventory { get; set; } = false;

        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public int CategoryId { get; set; }

        // Navigation
        public Category Category { get; set; } = null!;
        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
    }
}
