using System;
using System.ComponentModel.DataAnnotations;

namespace EnterprisePOS.Models.Entities
{
    public class WorkShift : ITenantEntity
    {
        [Key]
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        public DateTime StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        public bool IsClosed { get; set; }

        public decimal TotalSales { get; set; }
        public decimal TotalProfit { get; set; }
        
        public string? OpenedByUserId { get; set; }
        public ApplicationUser? OpenedByUser { get; set; }
    }
}
