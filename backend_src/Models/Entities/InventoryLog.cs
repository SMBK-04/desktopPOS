using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterprisePOS.Models.Entities
{
    public enum InventoryMovementType
    {
        Purchase = 0,       // Stock added via purchase
        Sale = 1,           // Stock reduced by sale
        Adjustment = 2,     // Manual admin adjustment
        Return = 3,         // Customer return adds stock back
        Damage = 4,         // Damaged goods reduction
        Transfer = 5        // Inter-location transfer
    }

    /// <summary>
    /// Tracks every change to product stock levels with full audit trail.
    /// </summary>
    public class InventoryLog : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        public int ProductId { get; set; }

        public InventoryMovementType MovementType { get; set; }

        public int QuantityBefore { get; set; }
        public int QuantityChanged { get; set; }    // Positive = added, Negative = removed
        public int QuantityAfter { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int? ReferenceId { get; set; }   // SaleId if movement caused by sale

        public string? UserId { get; set; }     // ApplicationUser.Id who made the change

        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;

        // Navigation
        public Product Product { get; set; } = null!;
        public ApplicationUser? User { get; set; }
    }
}
