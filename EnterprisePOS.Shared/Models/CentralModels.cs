using System;
using System.Collections.Generic;

namespace EnterprisePOS.Shared.Models
{
    public class Tenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string PasswordHash { get; set; } = string.Empty; // For Restaurant Admin
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }

    public class Subscription
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public int GracePeriodDays { get; set; }
        public bool IsPaymentDue { get; set; }
        public DateTime? LastPaidDate { get; set; }
        
        public Tenant? Tenant { get; set; }
    }

    public class License
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        
        public Tenant? Tenant { get; set; }
    }

    public class GlobalLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Action { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Guid? TenantId { get; set; }
    }

    public class SuperAdminUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}
