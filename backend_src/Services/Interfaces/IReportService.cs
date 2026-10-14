using EnterprisePOS.Models.ViewModels;

namespace EnterprisePOS.Services.Interfaces
{
    public interface IReportService
    {
        Task<ProfitLossViewModel> GetProfitLossAsync(DateTime from, DateTime to);
        Task<IEnumerable<TopProductViewModel>> GetTopProductsAsync(DateTime from, DateTime to, int top = 10);
        Task<IEnumerable<DailySalesViewModel>> GetDailySalesAsync(DateTime from, DateTime to);
        Task<byte[]> ExportSalesToCsvAsync(DateTime from, DateTime to);
        Task<byte[]> ExportSalesToPdfAsync(DateTime from, DateTime to);
        Task<byte[]> GenerateDayEndPdfAsync(int shiftId);
    }
}
