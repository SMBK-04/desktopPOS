using System.ComponentModel.DataAnnotations;

namespace EnterprisePOS.Models.Entities
{
    /// <summary>
    /// Stores a serialized held/paused cart so it can be resumed later.
    /// </summary>
    public class HeldOrder : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required, MaxLength(30)]
        public string HoldReference { get; set; } = string.Empty;   // e.g., HOLD-001

        [MaxLength(50)]
        public string? TableNumber { get; set; }

        [MaxLength(50)]
        public string? CustomerName { get; set; }

        public string CartJson { get; set; } = string.Empty;    // JSON-serialized cart items

        public string? CashierId { get; set; }
        public ApplicationUser? Cashier { get; set; }

        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;
    }
}
