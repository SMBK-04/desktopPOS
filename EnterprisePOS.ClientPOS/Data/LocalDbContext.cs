using EnterprisePOS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.ClientPOS.Data
{
    // ── Local-only Deal model (not synced to cloud) ──────────────────────────
    public class LocalDeal
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
        public List<LocalDealProduct> DealProducts { get; set; } = new();
    }

    public class LocalDealProduct
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DealId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public LocalDeal Deal { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
    // ─────────────────────────────────────────────────────────────────────────

    public class LocalDbContext : DbContext
    {
        public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options) { }

        public DbSet<LocalSettings> LocalSettings { get; set; }
        public DbSet<LocalUser> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<LocalDeal> Deals { get; set; }
        public DbSet<LocalDealProduct> DealProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<LocalSettings>().HasKey(x => x.Id);

            modelBuilder.Entity<LocalDeal>().HasKey(x => x.Id);
            modelBuilder.Entity<LocalDeal>().Property(x => x.Price).HasColumnType("TEXT");

            modelBuilder.Entity<LocalDealProduct>().HasKey(x => x.Id);
            modelBuilder.Entity<LocalDealProduct>()
                .HasOne(dp => dp.Deal)
                .WithMany(d => d.DealProducts)
                .HasForeignKey(dp => dp.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=localpos.db");
            }
        }
    }

    public class LocalDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<LocalDbContext>
    {
        public LocalDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LocalDbContext>();
            optionsBuilder.UseSqlite("Data Source=localpos.db");
            return new LocalDbContext(optionsBuilder.Options);
        }
    }
}

