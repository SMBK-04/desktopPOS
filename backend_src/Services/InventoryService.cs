using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EnterprisePOS.Services
{
    /// <summary>
    /// Handles inventory stock adjustments and movement history.
    /// Every stock change is logged for a complete audit trail.
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public InventoryService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<IEnumerable<InventoryLog>> GetLogsAsync(int? productId = null, int pageSize = 50)
        {
            var query = _db.InventoryLogs
                .Include(il => il.Product)
                .Include(il => il.User)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(il => il.ProductId == productId);

            return await query
                .OrderByDescending(il => il.CreatedAt)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task AdjustStockAsync(int productId, int quantity,
            InventoryMovementType movementType, string notes, string userId)
        {
            var product = await _db.Products.FindAsync(productId)
                ?? throw new KeyNotFoundException($"Product {productId} not found.");

            if (!product.TrackInventory) return;

            var before = product.CurrentStock;
            product.CurrentStock += quantity;
            product.CurrentStock = Math.Max(0, product.CurrentStock); // Never go negative

            var log = new InventoryLog
            {
                ProductId = productId,
                MovementType = movementType,
                QuantityBefore = before,
                QuantityChanged = quantity,
                QuantityAfter = product.CurrentStock,
                Notes = notes,
                UserId = userId,
                CreatedAt = TimeHelper.PKT
            };

            _db.InventoryLogs.Add(log);
            await _db.SaveChangesAsync();

            // Clear the product cache so POS screen updates immediately
            _cache.Remove("products_all");
        }

        public async Task<bool> IsInStockAsync(int productId, int requiredQuantity)
        {
            var product = await _db.Products.FindAsync(productId);
            return product != null && (!product.TrackInventory || product.CurrentStock >= requiredQuantity);
        }
    }
}
