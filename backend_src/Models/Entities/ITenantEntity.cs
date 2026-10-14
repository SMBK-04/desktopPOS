using System;

namespace EnterprisePOS.Models.Entities
{
    public interface ITenantEntity
    {
        Guid TenantId { get; set; }
    }
}
