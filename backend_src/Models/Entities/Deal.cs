using System;
using System.Collections.Generic;

namespace EnterprisePOS.Models.Entities
{
    public class Deal : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;

        // Navigation
        public ICollection<DealProduct> DealProducts { get; set; } = new List<DealProduct>();
    }
}
