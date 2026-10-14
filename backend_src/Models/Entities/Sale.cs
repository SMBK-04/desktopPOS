using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterprisePOS.Models.Entities
{
    public enum SaleStatus
    {
        Pending = 0,
        Completed = 1,
        Held = 2,
        Refunded = 3,
        PartialRefund = 4,
        Cancelled = 5
    }

    public enum OrderType
    {
        DineIn = 0,
        Takeaway = 1,
        Delivery = 2
    }

    public enum PaymentMethod
    {
        Cash = 0,
        Card = 1,
        DigitalWallet = 2,
        Split = 3
    }

    /// <summary>
    /// Represents a completed or in-progress sale transaction.
    /// </summary>
    public class Sale : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required, MaxLength(30)]
        public string InvoiceNumber { get; set; } = string.Empty;  // e.g., INV-20240120-001

        public SaleStatus Status { get; set; } = SaleStatus.Pending;
        public OrderType OrderType { get; set; } = OrderType.Takeaway;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [MaxLength(20)]
        public string? TableNumber { get; set; }

        [MaxLength(50)]
        public string? OrderToken { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal ServiceCharge { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ChangeGiven { get; set; } = 0;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;
        public DateTime? CompletedAt { get; set; }

        // Foreign Keys
        public int? CustomerId { get; set; }
        public string? CashierId { get; set; }  // ApplicationUser.Id

        // Navigation
        public Customer? Customer { get; set; }
        public ApplicationUser? Cashier { get; set; }
        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}
