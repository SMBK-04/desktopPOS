using EnterprisePOS.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.Data
{
    /// <summary>
    /// Seeds the database with initial roles, admin user, categories, and sample products.
    /// Called on application startup when the database is empty.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created and schema is ready
            try
            {
                await context.Database.EnsureCreatedAsync();
            }
            catch { }

            // ─── Seed Roles ──────────────────────────────────────────────
            var roles = new[] { "SuperAdmin", "Administrator", "Manager", "Cashier", "KitchenStaff" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ─── Seed / Update SuperAdmin User ──────────────────────────────
            const string superAdminEmail = "superadmin@SMBK.com";
            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
            if (superAdmin == null)
            {
                superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FullName = "Super Administrator",
                    EmployeeCode = "SUP-001",
                    EmailConfirmed = true,
                    IsActive = true,
                    PasswordPlain = "smbk1234"
                };
                var result = await userManager.CreateAsync(superAdmin, "smbk1234");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
            }
            else
            {
                superAdmin.PasswordPlain = "smbk1234";
                var token = await userManager.GeneratePasswordResetTokenAsync(superAdmin);
                await userManager.ResetPasswordAsync(superAdmin, token, "smbk1234");
                await userManager.UpdateAsync(superAdmin);
            }

            // ─── Seed Default Tenant ──────────────────────────────────────
            var defaultTenant = await context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync();
            if (defaultTenant == null)
            {
                defaultTenant = new Tenant
                {
                    Name = "Breadline",
                    BranchName = "North Karachi",
                    BranchAddress = "House R-461 Babul Islam Town Sector 9 North Karachi",
                    AdminEmail = "admin@pos.com",
                    AdminPasswordPlain = "Admin@123!",
                    SubscriptionEndDate = TimeHelper.PKT.AddYears(1),
                    IsBlocked = false,
                    IsOnlineOrderingEnabled = true,
                    IsDeliveryEnabled = true,
                    DeliveryRadiusKm = 5.0,
                    Latitude = 24.97438,
                    Longitude = 67.0556067
                };
                context.Tenants.Add(defaultTenant);
                await context.SaveChangesAsync();
            }

            // ─── Seed Admin User ─────────────────────────────────────────
            const string adminEmail = "admin@pos.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmployeeCode = "EMP-001",
                    EmailConfirmed = true,
                    IsActive = true,
                    TenantId = defaultTenant.Id,
                    PasswordPlain = "Admin@123!"
                };
                var result = await userManager.CreateAsync(admin, "Admin@123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Administrator");
            }

            // ─── Seed Demo Cashier ────────────────────────────────────────
            const string cashierEmail = "cashier@pos.com";
            if (await userManager.FindByEmailAsync(cashierEmail) == null)
            {
                var cashier = new ApplicationUser
                {
                    UserName = cashierEmail,
                    Email = cashierEmail,
                    FullName = "Jane Cashier",
                    EmployeeCode = "EMP-002",
                    EmailConfirmed = true,
                    IsActive = true,
                    TenantId = defaultTenant.Id,
                    PasswordPlain = "Cashier@123!"
                };
                var result = await userManager.CreateAsync(cashier, "Cashier@123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(cashier, "Cashier");
            }

            // ─── Seed Categories ─────────────────────────────────────────
            if (!await context.Categories.IgnoreQueryFilters().AnyAsync())
            {
                var categories = new List<Category>
                {
                    new() { Name = "Beverages",   IconClass = "bi bi-cup-hot-fill",   ColorHex = "#6366f1", TenantId = defaultTenant.Id },
                    new() { Name = "Food",         IconClass = "bi bi-egg-fried",       ColorHex = "#f59e0b", TenantId = defaultTenant.Id },
                    new() { Name = "Electronics",  IconClass = "bi bi-cpu-fill",        ColorHex = "#3b82f6", TenantId = defaultTenant.Id },
                    new() { Name = "Clothing",     IconClass = "bi bi-bag-fill",        ColorHex = "#ec4899", TenantId = defaultTenant.Id },
                    new() { Name = "Services",     IconClass = "bi bi-wrench-adjustable-circle-fill", ColorHex = "#10b981", TenantId = defaultTenant.Id },
                    new() { Name = "Stationery",   IconClass = "bi bi-pen-fill",        ColorHex = "#8b5cf6", TenantId = defaultTenant.Id },
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();

                // ─── Seed Sample Products ─────────────────────────────────
                var bevCat = categories[0];
                var foodCat = categories[1];
                var elecCat = categories[2];

                var products = new List<Product>
                {
                    new() { Name = "Espresso",       Barcode = "1000001", SKU = "BEV-001", ProductCode = "P001", CostPrice = 0.50m, SellingPrice = 3.50m, TaxRate = 5, CurrentStock = 500, LowStockThreshold = 50, CategoryId = bevCat.Id, TenantId = defaultTenant.Id },
                    new() { Name = "Cappuccino",      Barcode = "1000002", SKU = "BEV-002", ProductCode = "P002", CostPrice = 0.80m, SellingPrice = 4.50m, TaxRate = 5, CurrentStock = 300, LowStockThreshold = 30, CategoryId = bevCat.Id, TenantId = defaultTenant.Id },
                    new() { Name = "Green Tea",       Barcode = "1000003", SKU = "BEV-003", ProductCode = "P003", CostPrice = 0.30m, SellingPrice = 2.75m, TaxRate = 5, CurrentStock = 8,   LowStockThreshold = 20, CategoryId = bevCat.Id, TenantId = defaultTenant.Id },
                    new() { Name = "Mineral Water",   Barcode = "1000004", SKU = "BEV-004", ProductCode = "P004", CostPrice = 0.20m, SellingPrice = 1.50m, TaxRate = 0, CurrentStock = 200, LowStockThreshold = 30, CategoryId = bevCat.Id, TenantId = defaultTenant.Id },
                    new() { Name = "Club Sandwich",   Barcode = "1000005", SKU = "FOD-001", ProductCode = "P005", CostPrice = 2.50m, SellingPrice = 8.99m, TaxRate = 10, CurrentStock = 50,  LowStockThreshold = 10, CategoryId = foodCat.Id, TenantId = defaultTenant.Id },
                    new() { Name = "Margherita Pizza",Barcode = "1000006", SKU = "FOD-002", ProductCode = "P006", CostPrice = 4.00m, SellingPrice = 12.50m,TaxRate = 10, CurrentStock = 30,  LowStockThreshold = 5,  CategoryId = foodCat.Id, TenantId = defaultTenant.Id },
                    new() { Name = "Caesar Salad",    Barcode = "1000007", SKU = "FOD-003", ProductCode = "P007", CostPrice = 1.50m, SellingPrice = 7.25m, TaxRate = 10, CurrentStock = 3,   LowStockThreshold = 10, CategoryId = foodCat.Id, TenantId = defaultTenant.Id },
                    new() { Name = "Wireless Charger",Barcode = "1000008", SKU = "ELC-001", ProductCode = "P008", CostPrice = 8.00m, SellingPrice = 24.99m,TaxRate = 15, CurrentStock = 40,  LowStockThreshold = 5,  CategoryId = elecCat.Id, TenantId = defaultTenant.Id },
                    new() { Name = "USB-C Hub",       Barcode = "1000009", SKU = "ELC-002", ProductCode = "P009", CostPrice = 12.00m,SellingPrice = 35.99m,TaxRate = 15, CurrentStock = 25,  LowStockThreshold = 5,  CategoryId = elecCat.Id, TenantId = defaultTenant.Id },
                    new() { Name = "Laptop Stand",    Barcode = "1000010", SKU = "ELC-003", ProductCode = "P010", CostPrice = 10.00m,SellingPrice = 29.99m,TaxRate = 15, CurrentStock = 0,   LowStockThreshold = 3,  CategoryId = elecCat.Id, TenantId = defaultTenant.Id },
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            // ─── Seed Sample Customer ─────────────────────────────────────
            // ─── Ensure Tenant Admin credentials strictly point to the Tenant Administrator ───
            var allTenants = await context.Tenants.ToListAsync();
            var administrators = await userManager.GetUsersInRoleAsync("Administrator");
            foreach (var t in allTenants)
            {
                var admin = administrators.FirstOrDefault(u => u.TenantId == t.Id);
                if (admin != null)
                {
                    if (t.AdminEmail != admin.Email || (!string.IsNullOrEmpty(admin.PasswordPlain) && t.AdminPasswordPlain != admin.PasswordPlain))
                    {
                        t.AdminEmail = admin.Email ?? "";
                        if (!string.IsNullOrEmpty(admin.PasswordPlain))
                        {
                            t.AdminPasswordPlain = admin.PasswordPlain;
                        }
                    }
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
