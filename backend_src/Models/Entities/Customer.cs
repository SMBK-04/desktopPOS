using System.ComponentModel.DataAnnotations;

namespace EnterprisePOS.Models.Entities
{
    /// <summary>
    /// Customer entity for tracking buyers and their purchase history.
    /// </summary>
    public class Customer : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? CustomerCode { get; set; }   // e.g., CUST-00001

        public decimal LoyaltyPoints { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;

        // Navigation
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
