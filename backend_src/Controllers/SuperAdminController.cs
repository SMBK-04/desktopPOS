using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;

namespace EnterprisePOS.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public SuperAdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var tenants = await _db.Tenants.IgnoreQueryFilters().OrderBy(t => t.Name).ToListAsync();
            var adminUsers = await _userManager.GetUsersInRoleAsync("Administrator");

            var vmList = new List<TenantListItemViewModel>();
            foreach (var t in tenants)
            {
                var adminUser = adminUsers.FirstOrDefault(u => u.TenantId == t.Id);
                if (adminUser == null && !string.IsNullOrEmpty(t.AdminEmail))
                {
                    var found = await _userManager.FindByEmailAsync(t.AdminEmail);
                    if (found != null && await _userManager.IsInRoleAsync(found, "Administrator"))
                    {
                        adminUser = found;
                    }
                }

                var email = adminUser?.Email ?? t.AdminEmail ?? "—";
                var password = adminUser?.PasswordPlain ?? t.AdminPasswordPlain ?? "—";

                vmList.Add(new TenantListItemViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    BranchName = t.BranchName,
                    AdminEmail = email,
                    AdminPasswordPlain = password,
                    CreatedAt = t.CreatedAt,
                    SubscriptionEndDate = t.SubscriptionEndDate,
                    IsBlocked = t.IsBlocked,
                    BannerUrl = t.BannerUrl
                });
            }

            return View(vmList);
        }

        [HttpGet]
        public IActionResult CreateTenant()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTenant(CreateTenantViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Check if email already exists
            if (await _userManager.FindByEmailAsync(model.AdminEmail) != null)
            {
                ModelState.AddModelError("AdminEmail", "This email address is already registered.");
                return View(model);
            }

            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    string? bannerUrl = model.BannerUrl;
                    if (model.BannerFile != null && model.BannerFile.Length > 0)
                    {
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "banners");
                        Directory.CreateDirectory(uploadDir);
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.BannerFile.FileName)}";
                        var filePath = Path.Combine(uploadDir, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.BannerFile.CopyToAsync(stream);
                        }
                        bannerUrl = $"/uploads/banners/{fileName}";
                    }

                    // Create Tenant
                    var tenant = new Tenant
                    {
                        Name = model.Name,
                        BranchName = model.BranchName,
                        CreatedAt = TimeHelper.PKT,
                        SubscriptionEndDate = TimeHelper.PKT.AddMonths(1),
                        IsBlocked = false,
                        AdminEmail = model.AdminEmail,
                        AdminPasswordPlain = model.AdminPassword,
                        BranchAddress = model.BranchAddress,
                        BannerUrl = bannerUrl,
                        Latitude = model.Latitude != 0 ? model.Latitude : 24.97438,
                        Longitude = model.Longitude != 0 ? model.Longitude : 67.0556067,
                        DeliveryRadiusKm = model.DeliveryRadiusKm > 0 ? model.DeliveryRadiusKm : 5.0
                    };

                    _db.Tenants.Add(tenant);
                    await _db.SaveChangesAsync();

                    // Create Tenant Admin User
                    var adminUser = new ApplicationUser
                    {
                        UserName = model.AdminEmail,
                        Email = model.AdminEmail,
                        FullName = model.AdminFullName,
                        EmployeeCode = "OWNER-001",
                        EmailConfirmed = true,
                        IsActive = true,
                        TenantId = tenant.Id,
                        CreatedAt = TimeHelper.PKT,
                        PasswordPlain = model.AdminPassword
                    };

                    var userResult = await _userManager.CreateAsync(adminUser, model.AdminPassword);
                    if (!userResult.Succeeded)
                    {
                        foreach (var error in userResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        await transaction.RollbackAsync();
                        return View(model);
                    }

                    await _userManager.AddToRoleAsync(adminUser, "Administrator");
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Tenant '{model.Name}' created successfully with Administrator account.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                    return View(model);
                }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(Guid id)
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            tenant.IsBlocked = !tenant.IsBlocked;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Tenant '{tenant.Name}' block status updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubscription(Guid id, DateTime subscriptionEndDate)
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            tenant.SubscriptionEndDate = subscriptionEndDate;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Subscription end date for '{tenant.Name}' updated to {subscriptionEndDate:yyyy-MM-dd}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditTenant(Guid id)
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            var adminUsers = await _userManager.GetUsersInRoleAsync("Administrator");
            var adminUser = adminUsers.FirstOrDefault(u => u.TenantId == id);
            if (adminUser == null && !string.IsNullOrEmpty(tenant.AdminEmail))
            {
                var found = await _userManager.FindByEmailAsync(tenant.AdminEmail);
                if (found != null && await _userManager.IsInRoleAsync(found, "Administrator"))
                {
                    adminUser = found;
                }
            }
            
            var model = new EditTenantViewModel
            {
                Id = tenant.Id,
                Name = tenant.Name,
                BranchName = tenant.BranchName,
                SubscriptionEndDate = tenant.SubscriptionEndDate,
                AdminEmail = adminUser?.Email ?? tenant.AdminEmail ?? string.Empty,
                AdminFullName = adminUser?.FullName ?? string.Empty,
                BranchAddress = tenant.BranchAddress,
                BannerUrl = tenant.BannerUrl,
                Latitude = tenant.Latitude != 0 ? tenant.Latitude : 24.97438,
                Longitude = tenant.Longitude != 0 ? tenant.Longitude : 67.0556067,
                DeliveryRadiusKm = tenant.DeliveryRadiusKm > 0 ? tenant.DeliveryRadiusKm : 5.0
            };

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTenant(Guid id, EditTenantViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            var adminUsers = await _userManager.GetUsersInRoleAsync("Administrator");
            var adminUser = adminUsers.FirstOrDefault(u => u.TenantId == id);
            if (adminUser == null && !string.IsNullOrEmpty(tenant.AdminEmail))
            {
                var found = await _userManager.FindByEmailAsync(tenant.AdminEmail);
                if (found != null && await _userManager.IsInRoleAsync(found, "Administrator"))
                {
                    adminUser = found;
                }
            }

            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    tenant.Name = model.Name;
                    tenant.BranchName = model.BranchName;
                    tenant.SubscriptionEndDate = model.SubscriptionEndDate;
                    tenant.AdminEmail = model.AdminEmail;
                    tenant.BranchAddress = model.BranchAddress;
                    tenant.Latitude = model.Latitude != 0 ? model.Latitude : 24.97438;
                    tenant.Longitude = model.Longitude != 0 ? model.Longitude : 67.0556067;
                    tenant.DeliveryRadiusKm = model.DeliveryRadiusKm > 0 ? model.DeliveryRadiusKm : 5.0;

                    if (model.BannerFile != null && model.BannerFile.Length > 0)
                    {
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "banners");
                        Directory.CreateDirectory(uploadDir);
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.BannerFile.FileName)}";
                        var filePath = Path.Combine(uploadDir, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.BannerFile.CopyToAsync(stream);
                        }
                        tenant.BannerUrl = $"/uploads/banners/{fileName}";
                    }
                    else if (!string.IsNullOrEmpty(model.BannerUrl))
                    {
                        tenant.BannerUrl = model.BannerUrl;
                    }
                    if (!string.IsNullOrEmpty(model.AdminPassword))
                    {
                        tenant.AdminPasswordPlain = model.AdminPassword;
                    }

                    if (adminUser != null)
                    {
                        adminUser.Email = model.AdminEmail;
                        adminUser.UserName = model.AdminEmail;
                        adminUser.FullName = model.AdminFullName;

                        var updateResult = await _userManager.UpdateAsync(adminUser);
                        if (!updateResult.Succeeded)
                        {
                            foreach (var error in updateResult.Errors)
                            {
                                ModelState.AddModelError("", error.Description);
                            }
                            await transaction.RollbackAsync();
                            return View(model);
                        }

                        if (!string.IsNullOrEmpty(model.AdminPassword))
                        {
                            adminUser.PasswordPlain = model.AdminPassword;
                            var token = await _userManager.GeneratePasswordResetTokenAsync(adminUser);
                            var passwordResult = await _userManager.ResetPasswordAsync(adminUser, token, model.AdminPassword);
                            if (!passwordResult.Succeeded)
                            {
                                foreach (var error in passwordResult.Errors)
                                {
                                    ModelState.AddModelError("", error.Description);
                                }
                                await transaction.RollbackAsync();
                                return View(model);
                            }
                        }
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Tenant '{model.Name}' details updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                    return View(model);
                }
            });
        }
    }
}
