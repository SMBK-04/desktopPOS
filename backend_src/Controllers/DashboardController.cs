using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;
using EnterprisePOS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ISaleService _saleService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ISaleService saleService, 
            ApplicationDbContext db,
            ILogger<DashboardController> logger)
        {
            _saleService = saleService;
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // If SuperAdmin, show SaaS Platform Overview (Tenants, Growth, Subscriptions)
                if (User.IsInRole("SuperAdmin"))
                {
                    var tenants = await _db.Tenants.IgnoreQueryFilters()
                        .OrderByDescending(t => t.CreatedAt)
                        .ToListAsync();
                    
                    var now = TimeHelper.PKT;
                    var today = now.Date;
                    var thisMonth = new DateTime(now.Year, now.Month, 1);

                    var totalTenants = tenants.Count;
                    var activeTenants = tenants.Count(t => !t.IsBlocked && t.SubscriptionEndDate.Date >= today);
                    var blockedTenants = tenants.Count(t => t.IsBlocked);
                    var expiredTenants = tenants.Count(t => !t.IsBlocked && t.SubscriptionEndDate.Date < today);
                    var newTenantsThisMonth = tenants.Count(t => t.CreatedAt >= thisMonth);

                    // Last 6 months monthly tenant onboarding
                    var monthlyLabels = new List<string>();
                    var monthlyCounts = new List<int>();
                    for (int i = 5; i >= 0; i--)
                    {
                        var mDate = now.AddMonths(-i);
                        var startOfMonth = new DateTime(mDate.Year, mDate.Month, 1);
                        var endOfMonth = startOfMonth.AddMonths(1);
                        monthlyLabels.Add(startOfMonth.ToString("MMM yyyy"));
                        monthlyCounts.Add(tenants.Count(t => t.CreatedAt >= startOfMonth && t.CreatedAt < endOfMonth));
                    }

                    var superAdminVm = new SuperAdminDashboardViewModel
                    {
                        TotalTenants = totalTenants,
                        ActiveTenants = activeTenants,
                        BlockedTenants = blockedTenants,
                        ExpiredTenants = expiredTenants,
                        NewTenantsThisMonth = newTenantsThisMonth,
                        MonthlyLabels = monthlyLabels,
                        MonthlyNewTenantCounts = monthlyCounts,
                        RecentTenants = tenants.Take(6).ToList()
                    };

                    return View("SuperAdminDashboard", superAdminVm);
                }

                // For Tenant Admins, Managers & Cashiers, show store POS dashboard
                var stats = await _saleService.GetDashboardStatsAsync();
                
                var currentTenantId = _db.CurrentTenantId;
                if (currentTenantId != Guid.Empty)
                {
                    var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == currentTenantId);
                    if (tenant != null)
                    {
                        stats.IsDeliveryEnabled = tenant.IsDeliveryEnabled;
                        stats.IsOnlineOrderingEnabled = tenant.IsOnlineOrderingEnabled;
                        stats.IsTemporarilyClosed = tenant.IsTemporarilyClosed;
                        stats.StoreName = tenant.Name;
                    }
                }

                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard stats.");
                TempData["Error"] = "Failed to load dashboard data.";
                return View("Error");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager,Cashier")]
        public async Task<IActionResult> ToggleDelivery()
        {
            var tenantId = _db.CurrentTenantId;
            if (tenantId == Guid.Empty) return BadRequest();

            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null) return NotFound();

            tenant.IsDeliveryEnabled = !tenant.IsDeliveryEnabled;
            await _db.SaveChangesAsync();

            TempData["Success"] = tenant.IsDeliveryEnabled 
                ? "Online Delivery enabled! Customers can now place delivery orders." 
                : "Online Delivery disabled! Online orders will be Pickup only.";

            var returnUrl = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager,Cashier")]
        public async Task<IActionResult> ToggleStoreClose()
        {
            var tenantId = _db.CurrentTenantId;
            if (tenantId == Guid.Empty) return BadRequest();

            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null) return NotFound();

            tenant.IsTemporarilyClosed = !tenant.IsTemporarilyClosed;
            await _db.SaveChangesAsync();

            TempData["Success"] = tenant.IsTemporarilyClosed 
                ? "Store marked as Closed Early. Online orders are temporarily paused." 
                : "Store is now Open! Online orders are active.";

            var returnUrl = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager,Cashier")]
        public async Task<IActionResult> ToggleOnlineOrders()
        {
            var tenantId = _db.CurrentTenantId;
            if (tenantId == Guid.Empty) return BadRequest();

            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null) return NotFound();

            tenant.IsOnlineOrderingEnabled = !tenant.IsOnlineOrderingEnabled;
            await _db.SaveChangesAsync();

            TempData["Success"] = tenant.IsOnlineOrderingEnabled 
                ? "Online ordering is now Enabled for your store." 
                : "Online ordering has been Disabled for your store.";

            var returnUrl = Request.Headers["Referer"].ToString();
            return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }
    }
}
