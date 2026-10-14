using EnterprisePOS.Models.Entities;
using System.Threading.Tasks;

namespace EnterprisePOS.Services.Interfaces
{
    public interface IShiftService
    {
        Task<WorkShift?> GetActiveShiftAsync();
        Task<WorkShift> StartDayAsync(string userId);
        Task<WorkShift> EndDayAsync();
    }
}
