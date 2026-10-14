using System;

namespace EnterprisePOS.Models.Entities
{
    public class Tenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public bool IsBlocked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;
        public DateTime SubscriptionEndDate { get; set; }
        
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPasswordPlain { get; set; } = string.Empty;

        // Branch Location & Delivery Configuration
        public string? BranchAddress { get; set; }
        public string? BannerUrl { get; set; }
        public double Latitude { get; set; } = 24.97438;
        public double Longitude { get; set; } = 67.0556067;
        public double DeliveryRadiusKm { get; set; } = 5.0;

        // Store Operations & Online Delivery Controls
        public bool IsOnlineOrderingEnabled { get; set; } = true;
        public bool IsDeliveryEnabled { get; set; } = true;
        public bool IsTemporarilyClosed { get; set; } = false;
    }
}
