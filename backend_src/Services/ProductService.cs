using CsvHelper;
using CsvHelper.Configuration;
using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;
using EnterprisePOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace EnterprisePOS.Services
{
    /// <summary>
    /// Handles all product CRUD operations, search, and CSV import/export.
    /// Uses in-memory caching for the product catalog to reduce DB round-trips.
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private string CacheKey => $"products_all_{_db.CurrentTenantId}";

        public ProductService(ApplicationDbContext db, IMemoryCache cache, IWebHostEnvironment env)
        {
            _db = db;
            _cache = cache;
            _env = env;
        }

        private void InvalidateCache() => _cache.Remove(CacheKey);

        public async Task<IEnumerable<Product>> GetAllAsync(bool activeOnly = true)
        {
            if (!_cache.TryGetValue(CacheKey, out List<Product>? products) || products == null)
            {
                products = await _db.Products
                    .Include(p => p.Category)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                _cache.Set(CacheKey, products, TimeSpan.FromMinutes(5));
            }

            return activeOnly ? products.Where(p => p.IsActive) : products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> GetByBarcodeAsync(string barcode)
        {
            return await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Barcode == barcode);
        }

        public async Task<IEnumerable<Product>> SearchAsync(string term)
        {
            var lower = term.ToLower();
            return await _db.Products
                .Include(p => p.Category)
                .Where(p =>
                    p.Name.ToLower().Contains(lower) ||
                    (p.Barcode != null && p.Barcode.Contains(lower)) ||
                    (p.ProductCode != null && p.ProductCode.ToLower().Contains(lower)) ||
                    (p.SKU != null && p.SKU.ToLower().Contains(lower)))
                .Take(20)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        {
            return await _db.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockAsync()
        {
            return await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.TrackInventory && p.CurrentStock <= p.LowStockThreshold)
                .OrderBy(p => p.CurrentStock)
                .ToListAsync();
        }

        public async Task<Product> CreateAsync(ProductCreateViewModel model, string? imagePath)
        {
            var product = new Product
            {
                Name = model.Name,
                ProductCode = model.ProductCode,
                Barcode = model.Barcode,
                SKU = model.SKU,
                Description = model.Description,
                ProductType = model.ProductType,
                CostPrice = model.CostPrice,
                SellingPrice = model.SellingPrice,
                TaxRate = model.TaxRate,
                CurrentStock = model.CurrentStock,
                LowStockThreshold = model.LowStockThreshold,
                ReorderLevel = model.ReorderLevel,
                CategoryId = model.CategoryId,
                IsActive = model.IsActive,
                TrackInventory = model.TrackInventory,
                ImagePath = imagePath,
                CreatedAt = TimeHelper.PKT
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            InvalidateCache();
            return product;
        }

        public async Task UpdateAsync(int id, ProductEditViewModel model, string? imagePath)
        {
            var product = await _db.Products.FindAsync(id)
                ?? throw new KeyNotFoundException($"Product {id} not found.");

            product.Name = model.Name;
            product.ProductCode = model.ProductCode;
            product.Barcode = model.Barcode;
            product.SKU = model.SKU;
            product.Description = model.Description;
            product.ProductType = model.ProductType;
            product.CostPrice = model.CostPrice;
            product.SellingPrice = model.SellingPrice;
            product.TaxRate = model.TaxRate;
            product.LowStockThreshold = model.LowStockThreshold;
            product.ReorderLevel = model.ReorderLevel;
            product.CategoryId = model.CategoryId;
            product.IsActive = model.IsActive;
            product.TrackInventory = model.TrackInventory;
            product.UpdatedAt = TimeHelper.PKT;

            if (imagePath != null)
                product.ImagePath = imagePath;

            await _db.SaveChangesAsync();
            InvalidateCache();
        }

        public async Task ToggleActiveAsync(int id)
        {
            var product = await _db.Products.FindAsync(id)
                ?? throw new KeyNotFoundException($"Product {id} not found.");

            product.IsActive = !product.IsActive;
            await _db.SaveChangesAsync();
            InvalidateCache();
        }

        public async Task<bool> HardDeleteAsync(int id)
        {
            var product = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Product {id} not found.");

            // Remove any associated foreign key records so deletion succeeds unconditionally
            var dealProducts = await _db.DealProducts.IgnoreQueryFilters().Where(dp => dp.ProductId == id).ToListAsync();
            if (dealProducts.Any()) _db.DealProducts.RemoveRange(dealProducts);

            var logs = await _db.InventoryLogs.IgnoreQueryFilters().Where(l => l.ProductId == id).ToListAsync();
            if (logs.Any()) _db.InventoryLogs.RemoveRange(logs);

            var saleItems = await _db.SaleItems.IgnoreQueryFilters().Where(si => si.ProductId == id).ToListAsync();
            if (saleItems.Any()) _db.SaleItems.RemoveRange(saleItems);

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            InvalidateCache();
            return true;
        }

        public async Task<byte[]> ExportToCsvAsync()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync();

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteHeader<ProductCsvRecord>();
            await csv.NextRecordAsync();

            foreach (var p in products)
            {
                csv.WriteRecord(new ProductCsvRecord
                {
                    Name = p.Name,
                    ProductCode = p.ProductCode ?? "",
                    Barcode = p.Barcode ?? "",
                    SKU = p.SKU ?? "",
                    Category = p.Category.Name,
                    CostPrice = p.CostPrice,
                    SellingPrice = p.SellingPrice,
                    TaxRate = p.TaxRate,
                    CurrentStock = p.CurrentStock,
                    LowStockThreshold = p.LowStockThreshold
                });
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();
            return ms.ToArray();
        }

        public async Task<int> ImportFromCsvAsync(Stream csvStream)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HeaderValidated = null, MissingFieldFound = null };
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<ProductCsvRecord>().ToList();
            int imported = 0;

            foreach (var rec in records)
            {
                if (string.IsNullOrWhiteSpace(rec.Name)) continue;

                var category = await _db.Categories
                    .FirstOrDefaultAsync(c => c.Name == rec.Category)
                    ?? await _db.Categories.FirstAsync();

                var existing = await _db.Products.FirstOrDefaultAsync(p => p.Barcode == rec.Barcode && !string.IsNullOrEmpty(rec.Barcode));

                if (existing != null)
                {
                    existing.SellingPrice = rec.SellingPrice;
                    existing.CostPrice = rec.CostPrice;
                    existing.CurrentStock = rec.CurrentStock;
                }
                else
                {
                    _db.Products.Add(new Product
                    {
                        Name = rec.Name,
                        ProductCode = rec.ProductCode,
                        Barcode = rec.Barcode,
                        SKU = rec.SKU,
                        CategoryId = category.Id,
                        CostPrice = rec.CostPrice,
                        SellingPrice = rec.SellingPrice,
                        TaxRate = rec.TaxRate,
                        CurrentStock = rec.CurrentStock,
                        LowStockThreshold = rec.LowStockThreshold
                    });
                    imported++;
                }
            }

            await _db.SaveChangesAsync();
            InvalidateCache();
            return imported;
        }
    }

    // CSV record shape for CsvHelper mapping
    public class ProductCsvRecord
    {
        public string Name { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal TaxRate { get; set; }
        public int CurrentStock { get; set; }
        public int LowStockThreshold { get; set; }
    }
}
