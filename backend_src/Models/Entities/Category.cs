using System.ComponentModel.DataAnnotations;

namespace EnterprisePOS.Models.Entities
{
    /// <summary>
    /// Represents a product category for organizing inventory items.
    /// </summary>
    public class Category : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? IconClass { get; set; }   // FontAwesome / Bootstrap icon class
        public string? ColorHex { get; set; }    // Category accent color for UI
        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;

        // Navigation
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
