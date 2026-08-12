using EnterprisePOS.ServerAPI.Data;
using EnterprisePOS.Shared.Models;
using EnterprisePOS.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ServerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenantsController : ControllerBase
    {
        private readonly CentralDbContext _context;
        private readonly LicenseService _licenseService;

        public TenantsController(CentralDbContext context, LicenseService licenseService)
        {
            _context = context;
            _licenseService = licenseService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] TenantLoginDto dto)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Email.ToLower() == dto.Email.ToLower());

            if (tenant == null || tenant.PasswordHash != dto.Password)
                return Unauthorized();

            return Ok(new { TenantId = tenant.Id });
        }
        [HttpPost("provision")]
        public async Task<IActionResult> ProvisionTenant([FromBody] ProvisionTenantDto dto)
        {
            var tenant = new Tenant
            {
                CompanyName = dto.CompanyName,
                Email = dto.Email,
                BranchName = dto.BranchName,
                PasswordHash = dto.AdminPassword
            };
            _context.Tenants.Add(tenant);

            var subscription = new Subscription
            {
                TenantId = tenant.Id,
                StartDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddMonths(1),
                GracePeriodDays = 3,
                IsPaymentDue = false
            };
            _context.Subscriptions.Add(subscription);

            var expiry = subscription.DueDate.AddDays(subscription.GracePeriodDays);
            var token = _licenseService.GenerateLicenseToken(tenant.Id.ToString(), expiry, false);

            var license = new License
            {
                TenantId = tenant.Id,
                LicenseKey = token,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = expiry,
                IsActive = true
            };
            _context.Licenses.Add(license);

            await _context.SaveChangesAsync();
            return Ok(new { TenantId = tenant.Id, LicenseToken = token });
        }

        [HttpGet]
        public async Task<IActionResult> GetTenants()
        {
            var tenants = await _context.Tenants.Include(t => t.Subscriptions).ToListAsync();
            return Ok(tenants);
        }

        [HttpPost("{tenantId}/mark-due")]
        public async Task<IActionResult> MarkPaymentDue(Guid tenantId)
        {
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId);
            if (sub == null) return NotFound();

            sub.IsPaymentDue = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{tenantId}/mark-paid")]
        public async Task<IActionResult> MarkPaid(Guid tenantId)
        {
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId);
            if (sub == null) return NotFound();

            sub.IsPaymentDue = false;
            sub.LastPaidDate = DateTime.UtcNow;
            sub.DueDate = sub.DueDate.AddMonths(1); // Renew for a month
            
            // Renew License
            var license = await _context.Licenses.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.IsActive);
            var expiry = sub.DueDate.AddDays(sub.GracePeriodDays);
            var token = _licenseService.GenerateLicenseToken(tenantId.ToString(), expiry, false);
            
            if (license != null)
            {
                license.LicenseKey = token;
                license.ExpiresAt = expiry;
            }
            else
            {
                _context.Licenses.Add(new License { TenantId = tenantId, LicenseKey = token, ExpiresAt = expiry, IsActive = true, IssuedAt = DateTime.UtcNow });
            }

            await _context.SaveChangesAsync();
            return Ok(new { LicenseToken = token });
        }
        [HttpDelete("{tenantId}")]
        public async Task<IActionResult> DeleteTenant(Guid tenantId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return NotFound();

            // Remove related records
            var subs = _context.Subscriptions.Where(s => s.TenantId == tenantId);
            _context.Subscriptions.RemoveRange(subs);

            var licenses = _context.Licenses.Where(l => l.TenantId == tenantId);
            _context.Licenses.RemoveRange(licenses);

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{tenantId}/change-password")]
        public async Task<IActionResult> ChangePassword(Guid tenantId, [FromBody] ChangePasswordDto dto)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return NotFound();

            tenant.PasswordHash = dto.NewPassword;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class ProvisionTenantDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class TenantLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
