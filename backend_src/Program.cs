using EnterprisePOS.Data;
using EnterprisePOS.Hubs;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Services;
using EnterprisePOS.Services.Interfaces;
using EnterprisePOS.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog Structured Logging ──────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/pos-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Database (Offline SQLite) ────────────────────────────────────────────────
var dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnterprisePOS_Offline");
if (!Directory.Exists(dbFolder))
{
    Directory.CreateDirectory(dbFolder);
}
var dbPath = Path.Combine(dbFolder, "enterprisepos.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ─── ASP.NET Core Identity ───────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, EnterprisePOS.Helpers.CustomClaimsPrincipalFactory>();

// ─── Cookie Auth Settings ─────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
});

// ─── In-Memory Caching ────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// ─── MVC + Razor Views ───────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<EnterprisePOS.Filters.SubscriptionActiveFilter>();
});

// ─── SignalR ─────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── CORS (allow E-Commerce sites & storefronts to call POS API) ──────
builder.Services.AddCors(options =>
{
    options.AddPolicy("EcomPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ─── Application Services (Dependency Injection) ─────────────────────────────
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ─── Background Workers ───────────────────────────────────────────────────────
builder.Services.AddHostedService<DatabaseBackupWorker>();

// ─── Anti-Forgery (CSRF Protection) ──────────────────────────────────────────
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

// Persist login cookies across restarts
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "Keys");
if (!Directory.Exists(keysPath))
{
    Directory.CreateDirectory(keysPath);
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("BreadlineShared");

var app = builder.Build();

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Dashboard/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles();
app.UseSerilogRequestLogging();

app.UseRouting();
app.UseCors("EcomPolicy");
app.UseAuthentication();
app.UseAuthorization();

// ─── Routes ──────────────────────────────────────────────────────────────────
app.MapGet("/ping", () => Results.Ok(new { status = "alive", time = EnterprisePOS.TimeHelper.PKT }));

app.MapGet("/keep-alive", () => Results.Ok(new { status = "active", message = "Offline POS active.", time = DateTime.UtcNow }));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// ─── SignalR Hubs ────────────────────────────────────────────────────────
app.MapHub<KitchenHub>("/hubs/kitchen");
app.MapHub<EnterprisePOS.Hubs.NotificationHub>("/hubs/notifications");

// ─── Seed Database ────────────────────────────────────────────────────────────
try
{
    await DbSeeder.SeedAsync(app.Services);
    Log.Information("Database seeded successfully.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Database seeding failed.");
}

Log.Information("Enterprise POS starting up...");
app.Run();
