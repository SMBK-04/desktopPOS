using EnterprisePOS.Models.Entities;

namespace EnterprisePOS.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<InventoryLog>> GetLogsAsync(int? productId = null, int pageSize = 50);
        Task AdjustStockAsync(int productId, int quantity, InventoryMovementType movementType, string notes, string userId);
        Task<bool> IsInStockAsync(int productId, int requiredQuantity);
    }
}
