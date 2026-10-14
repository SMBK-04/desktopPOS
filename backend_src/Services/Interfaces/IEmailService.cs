using System.Threading.Tasks;
using EnterprisePOS.Models.Entities;

namespace EnterprisePOS.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task<bool> SendOrderConfirmationEmailAsync(OnlineOrder order, string restaurantName);
    }
}
