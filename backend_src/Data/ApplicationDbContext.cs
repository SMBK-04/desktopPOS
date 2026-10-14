using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EnterprisePOS.Models.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace EnterprisePOS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
            : base(options) 
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid CurrentTenantId
        {
            get
            {
                var tenantIdClaim = _httpContextAccessor?.HttpContext?.User?.FindFirst("TenantId")?.Value;
                if (Guid.TryParse(tenantIdClaim, out var tenantId))
                {
                    return tenantId;
                }
                return Guid.Empty;
            }
        }

        public bool IsSuperAdmin
        {
            get
            {
                return _httpContextAccessor?.HttpContext?.User?.IsInRole("SuperAdmin") ?? false;
            }
        }

        // ─── Core POS Tables ────────────────────────────────────────────
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<InventoryLog> InventoryLogs { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<HeldOrder> HeldOrders { get; set; }
        public DbSet<WorkShift> WorkShifts { get; set; }
        public DbSet<OnlineOrder> OnlineOrders { get; set; }
        public DbSet<OnlineOrderReview> OnlineOrderReviews { get; set; }
        public DbSet<Deal> Deals { get; set; }
        public DbSet<DealProduct> DealProducts { get; set; }

        public override int SaveChanges()
        {
            SetTenantIds();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetTenantIds();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void SetTenantIds()
        {
            var tenantId = CurrentTenantId;
            if (tenantId == Guid.Empty) return;

            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.TenantId == Guid.Empty)
                    {
                        entry.Entity.TenantId = tenantId;
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ─── Tenant Configuration ─────────────────────────────────────
            builder.Entity<Tenant>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Name).IsRequired().HasMaxLength(150);
            });

            // ─── Category ───────────────────────────────────────────────
            builder.Entity<Category>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            });

            // ─── Product ─────────────────────────────────────────────────
            builder.Entity<Product>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).IsRequired().HasMaxLength(150);
                e.HasIndex(p => p.Barcode);
                e.HasIndex(p => p.ProductCode);

                e.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ─── Customer ────────────────────────────────────────────────
            builder.Entity<Customer>(e =>
            {
                e.HasKey(c => c.Id);
                e.HasIndex(c => c.Email);
                e.Property(c => c.LoyaltyPoints).HasPrecision(18, 2);
            });

            // ─── Sale ────────────────────────────────────────────────────
            builder.Entity<Sale>(e =>
            {
                e.HasKey(s => s.Id);
                e.HasIndex(s => s.InvoiceNumber).IsUnique();

                e.HasOne(s => s.Customer)
                    .WithMany(c => c.Sales)
                    .HasForeignKey(s => s.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(s => s.Cashier)
                    .WithMany(u => u.Sales)
                    .HasForeignKey(s => s.CashierId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── SaleItem ────────────────────────────────────────────────
            builder.Entity<SaleItem>(e =>
            {
                e.HasKey(si => si.Id);

                e.HasOne(si => si.Sale)
                    .WithMany(s => s.SaleItems)
                    .HasForeignKey(si => si.SaleId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(si => si.Product)
                    .WithMany(p => p.SaleItems)
                    .HasForeignKey(si => si.ProductId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                e.HasOne(si => si.Deal)
                    .WithMany()
                    .HasForeignKey(si => si.DealId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });

            // ─── InventoryLog ────────────────────────────────────────────
            builder.Entity<InventoryLog>(e =>
            {
                e.HasKey(il => il.Id);

                e.HasOne(il => il.Product)
                    .WithMany(p => p.InventoryLogs)
                    .HasForeignKey(il => il.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(il => il.User)
                    .WithMany(u => u.InventoryLogs)
                    .HasForeignKey(il => il.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── Expense ─────────────────────────────────────────────────
            builder.Entity<Expense>(e =>
            {
                e.HasKey(ex => ex.Id);

                e.HasOne(ex => ex.RecordedBy)
                    .WithMany(u => u.Expenses)
                    .HasForeignKey(ex => ex.RecordedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── HeldOrder ───────────────────────────────────────────────
            builder.Entity<HeldOrder>(e =>
            {
                e.HasKey(h => h.Id);
                e.HasIndex(h => h.HoldReference).IsUnique();

                e.HasOne(h => h.Cashier)
                    .WithMany()
                    .HasForeignKey(h => h.CashierId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── OnlineOrder ─────────────────────────────────────────────
            builder.Entity<OnlineOrder>(e =>
            {
                e.HasKey(o => o.Id);
                e.HasIndex(o => o.OrderNumber).IsUnique();
                e.Property(o => o.TotalAmount).HasPrecision(18, 4);
                e.Property(o => o.CustomerLat).HasPrecision(9, 6);
                e.Property(o => o.CustomerLng).HasPrecision(9, 6);
                e.Property(o => o.DistanceKm).HasPrecision(5, 2);

                e.HasOne(o => o.LinkedSale)
                    .WithMany()
                    .HasForeignKey(o => o.LinkedSaleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ─── OnlineOrderReview ────────────────────────────────────────
            builder.Entity<OnlineOrderReview>(e =>
            {
                e.HasKey(r => r.Id);
                e.HasOne(r => r.Order)
                    .WithMany(o => o.Reviews)
                    .HasForeignKey(r => r.OnlineOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Deals Configurations ─────────────────────────────────────
            builder.Entity<Deal>(e =>
            {
                e.HasKey(d => d.Id);
                e.Property(d => d.Price).HasPrecision(18, 2);
            });

            builder.Entity<DealProduct>(e =>
            {
                e.HasKey(dp => dp.Id);
                
                e.HasOne(dp => dp.Deal)
                    .WithMany(d => d.DealProducts)
                    .HasForeignKey(dp => dp.DealId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(dp => dp.Product)
                    .WithMany()
                    .HasForeignKey(dp => dp.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Global Query Filters for Multi-Tenancy ────────────────────
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var body = System.Linq.Expressions.Expression.Or(
                        System.Linq.Expressions.Expression.Equal(
                            System.Linq.Expressions.Expression.Property(parameter, nameof(ITenantEntity.TenantId)),
                            System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Constant(this), nameof(CurrentTenantId))
                        ),
                        System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Constant(this), nameof(IsSuperAdmin))
                    );
                    var lambda = System.Linq.Expressions.Expression.Lambda(body, parameter);
                    builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }

            // ─── WorkShift Configurations ─────────────────────────────────
            builder.Entity<WorkShift>(e =>
            {
                e.Property(w => w.TotalSales).HasPrecision(18, 2);
                e.Property(w => w.TotalProfit).HasPrecision(18, 2);
            });

            // ─── Child Entity Query Filters to resolve warnings ─────────────
            builder.Entity<SaleItem>().HasQueryFilter(si => si.Sale.TenantId == CurrentTenantId || IsSuperAdmin);
            builder.Entity<DealProduct>().HasQueryFilter(dp => dp.Deal.TenantId == CurrentTenantId || IsSuperAdmin);
            builder.Entity<OnlineOrderReview>().HasQueryFilter(r => r.Order.TenantId == CurrentTenantId || IsSuperAdmin);
        }
    }
}
