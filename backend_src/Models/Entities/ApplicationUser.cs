using Microsoft.AspNetCore.Identity;

namespace EnterprisePOS.Models.Entities
{
    /// <summary>
    /// Extended Identity user with POS-specific profile fields.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarPath { get; set; }
        public string? EmployeeCode { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;
        public DateTime? LastLoginAt { get; set; }
        public string? PasswordPlain { get; set; }

        // Tenant Association
        public Guid? TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        // Navigation
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
