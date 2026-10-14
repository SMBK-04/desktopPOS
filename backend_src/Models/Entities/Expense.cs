using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterprisePOS.Models.Entities
{
    public enum ExpenseCategory
    {
        Utilities = 0,
        Rent = 1,
        Salaries = 2,
        Supplies = 3,
        Marketing = 4,
        Maintenance = 5,
        Other = 6
    }

    /// <summary>
    /// Business expense tracking for P&L reporting.
    /// </summary>
    public class Expense : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ExpenseCategory Category { get; set; } = ExpenseCategory.Other;

        [Column(TypeName = "decimal(18,4)")]
        public decimal Amount { get; set; }

        public DateTime ExpenseDate { get; set; } = TimeHelper.PKT;
        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;

        public string? RecordedByUserId { get; set; }
        public ApplicationUser? RecordedBy { get; set; }
    }
}
