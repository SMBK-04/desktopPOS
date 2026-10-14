using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.Controllers
{
    public class AccountController : Controller
    {   
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "Invalid email or password, or your account has been deactivated.");
                return View(model);
            }

            if (user.TenantId.HasValue)
            {
                var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == user.TenantId.Value);
                if (tenant != null && tenant.IsBlocked)
                {
                    ModelState.AddModelError("", "This restaurant account has been blocked by the system administrator. Access is disabled.");
                    return View(model);
                }
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: true, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLoginAt = TimeHelper.PKT;
                await _userManager.UpdateAsync(user);

                return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Dashboard")!);
            }

            if (result.IsLockedOut)
                ModelState.AddModelError("", "Account is locked out. Please try again later.");
            else
                ModelState.AddModelError("", "Invalid email or password.");

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Users()
        {
            var tenantId = _db.CurrentTenantId;
            var users = await _userManager.Users
                .Include(u => u.Tenant)
                .Where(u => u.TenantId == tenantId)
                .ToListAsync();
            var vmList = new List<UserListViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                vmList.Add(new UserListViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email!,
                    PasswordPlain = !string.IsNullOrEmpty(u.PasswordPlain) 
                        ? u.PasswordPlain 
                        : (u.Tenant?.AdminPasswordPlain ?? "Admin@123"),
                    EmployeeCode = u.EmployeeCode,
                    Role = string.Join(", ", roles),
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                });
            }

            return View(vmList);
        }

        private async Task<string> GetAdminDomainSuffixAsync()
        {
            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser != null && !string.IsNullOrEmpty(adminUser.Email))
            {
                var idx = adminUser.Email.IndexOf('@');
                if (idx >= 0)
                {
                    return adminUser.Email.Substring(idx);
                }
            }
            return "";
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.DomainSuffix = await GetAdminDomainSuffixAsync();
            var vm = new UserCreateViewModel
            {
                AvailableRoles = await _roleManager.Roles
                    .Where(r => r.Name != "SuperAdmin")
                    .Select(r => r.Name!).ToListAsync()
            };
            return View(vm);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserCreateViewModel model)
        {
            var domainSuffix = await GetAdminDomainSuffixAsync();

            // Reconstruct the full email by appending the domain suffix
            if (!string.IsNullOrEmpty(model.Email))
            {
                model.Email = model.Email.Trim() + domainSuffix;
                ModelState.Remove(nameof(model.Email));
                TryValidateModel(model);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.DomainSuffix = domainSuffix;
                model.AvailableRoles = await _roleManager.Roles
                    .Where(r => r.Name != "SuperAdmin")
                    .Select(r => r.Name!).ToListAsync();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmployeeCode = model.EmployeeCode,
                IsActive = model.IsActive,
                EmailConfirmed = true,
                TenantId = _db.CurrentTenantId,
                PasswordPlain = model.Password
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                ViewBag.DomainSuffix = domainSuffix;
                model.AvailableRoles = await _roleManager.Roles
                    .Where(r => r.Name != "SuperAdmin")
                    .Select(r => r.Name!).ToListAsync();
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["Success"] = $"User '{model.FullName}' created successfully.";
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "User ID and New Password are required.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Ensure the user belongs to the current tenant (or caller is SuperAdmin)
            if (user.TenantId != _db.CurrentTenantId && !User.IsInRole("SuperAdmin"))
            {
                TempData["Error"] = "Unauthorized to change password for this user.";
                return RedirectToAction(nameof(Users));
            }

            // Remove existing password & add new password via reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

            // Update PasswordPlain on ApplicationUser
            user.PasswordPlain = newPassword;
            await _userManager.UpdateAsync(user);

            // If this user is an Administrator or the tenant's admin, update Tenant.AdminPasswordPlain so SuperAdmin sees the updated password
            if (user.TenantId.HasValue)
            {
                var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == user.TenantId.Value);
                if (tenant != null)
                {
                    var isTenantAdmin = await _userManager.IsInRoleAsync(user, "Administrator") || user.Email == tenant.AdminEmail;
                    if (isTenantAdmin)
                    {
                        tenant.AdminPasswordPlain = newPassword;
                        await _db.SaveChangesAsync();
                    }
                }
            }

            TempData["Success"] = $"Password for '{user.FullName}' updated successfully.";
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"User {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Users));
        }
    }
}
