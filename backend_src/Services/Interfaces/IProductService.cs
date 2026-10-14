using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;

namespace EnterprisePOS.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync(bool activeOnly = true);
        Task<Product?> GetByIdAsync(int id);
        Task<Product?> GetByBarcodeAsync(string barcode);
        Task<IEnumerable<Product>> SearchAsync(string term);
        Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetLowStockAsync();
        Task<Product> CreateAsync(ProductCreateViewModel model, string? imagePath);
        Task UpdateAsync(int id, ProductEditViewModel model, string? imagePath);
        Task ToggleActiveAsync(int id);
        Task<bool> HardDeleteAsync(int id);
        Task<byte[]> ExportToCsvAsync();
        Task<int> ImportFromCsvAsync(Stream csvStream);
    }
}
