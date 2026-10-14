using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;

namespace EnterprisePOS.Services.Interfaces
{
    public interface ISaleService
    {
        Task<Sale?> GetByIdAsync(int id);
        Task<IEnumerable<Sale>> GetTodaysSalesAsync();
        Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime from, DateTime to);
        Task<Sale> ProcessCheckoutAsync(CheckoutViewModel model, string cashierId);
        Task<Sale> RefundSaleAsync(int saleId, string userId);
        Task HoldOrderAsync(HoldOrderViewModel model, string cashierId);
        Task<IEnumerable<HeldOrder>> GetHeldOrdersAsync();
        Task<HeldOrder?> GetHeldOrderAsync(int id);
        Task DeleteHeldOrderAsync(int id);
        Task<DashboardStatsViewModel> GetDashboardStatsAsync();
        string GenerateInvoiceNumber();
    }
}
