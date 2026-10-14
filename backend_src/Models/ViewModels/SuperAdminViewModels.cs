using System.ComponentModel.DataAnnotations;

namespace EnterprisePOS.Models.ViewModels
{
    public class CreateTenantViewModel
    {
        [Required, Display(Name = "Business Name")]
        public string Name { get; set; } = string.Empty;

        [Required, Display(Name = "Branch/Location Name")]
        public string BranchName { get; set; } = string.Empty;

        [Required, EmailAddress, Display(Name = "Administrator Email")]
        public string AdminEmail { get; set; } = string.Empty;

        [Required, Display(Name = "Administrator Full Name")]
        public string AdminFullName { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [Display(Name = "Administrator Password")]
        public string AdminPassword { get; set; } = string.Empty;

        [Display(Name = "Branch Street Address / Location")]
        public string? BranchAddress { get; set; }

        [Display(Name = "Branch Latitude")]
        public double Latitude { get; set; } = 24.97438;

        [Display(Name = "Branch Longitude")]
        public double Longitude { get; set; } = 67.0556067;

        [Display(Name = "Delivery Radius (KM)")]
        public double DeliveryRadiusKm { get; set; } = 5.0;

        [Display(Name = "Restaurant Cover Banner URL (Optional)")]
        public string? BannerUrl { get; set; }

        [Display(Name = "Upload Restaurant Cover Banner (Optional)")]
        public Microsoft.AspNetCore.Http.IFormFile? BannerFile { get; set; }
    }

    public class EditTenantViewModel
    {
        public Guid Id { get; set; }

        [Required, Display(Name = "Business Name")]
        public string Name { get; set; } = string.Empty;

        [Required, Display(Name = "Branch/Location Name")]
        public string BranchName { get; set; } = string.Empty;

        [Required, EmailAddress, Display(Name = "Administrator Email")]
        public string AdminEmail { get; set; } = string.Empty;

        [Required, Display(Name = "Administrator Full Name")]
        public string AdminFullName { get; set; } = string.Empty;

        [Display(Name = "New Administrator Password")]
        public string? AdminPassword { get; set; }

        [Required, DataType(DataType.Date), Display(Name = "Subscription Expiry Date")]
        public DateTime SubscriptionEndDate { get; set; }

        [Display(Name = "Branch Street Address / Location")]
        public string? BranchAddress { get; set; }

        [Display(Name = "Branch Latitude")]
        public double Latitude { get; set; } = 24.97438;

        [Display(Name = "Branch Longitude")]
        public double Longitude { get; set; } = 67.0556067;

        [Display(Name = "Delivery Radius (KM)")]
        public double DeliveryRadiusKm { get; set; } = 5.0;

        [Display(Name = "Restaurant Cover Banner URL (Optional)")]
        public string? BannerUrl { get; set; }

        [Display(Name = "Upload Restaurant Cover Banner (Optional)")]
        public Microsoft.AspNetCore.Http.IFormFile? BannerFile { get; set; }
    }

    public class SuperAdminDashboardViewModel
    {
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int BlockedTenants { get; set; }
        public int ExpiredTenants { get; set; }
        public int NewTenantsThisMonth { get; set; }

        public List<string> MonthlyLabels { get; set; } = new();
        public List<int> MonthlyNewTenantCounts { get; set; } = new();

        public List<EnterprisePOS.Models.Entities.Tenant> RecentTenants { get; set; } = new();
    }

    public class TenantListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPasswordPlain { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
        public bool IsBlocked { get; set; }
        public string? BannerUrl { get; set; }
    }
}
