using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterprisePOS.Models.Entities
{
    /// <summary>
    /// Individual line item within a sale transaction.
    /// </summary>
    public class SaleItem
    {
        public int Id { get; set; }

        public int SaleId { get; set; }
        public int? ProductId { get; set; }
        public Guid? DealId { get; set; }

        [Required, MaxLength(150)]
        public string ProductName { get; set; } = string.Empty;  // Snapshot at time of sale

        [MaxLength(200)]
        public string? Modifiers { get; set; }  // e.g., "Extra cheese, No onions"

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal LineTotal { get; set; }  // (UnitPrice * Qty) - Discount + Tax

        // Navigation
        public Sale Sale { get; set; } = null!;
        public Product? Product { get; set; }
        public Deal? Deal { get; set; }
    }
}
