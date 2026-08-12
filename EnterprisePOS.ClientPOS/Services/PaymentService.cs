using System;
using System.Threading.Tasks;

namespace EnterprisePOS.ClientPOS.Services
{
    public class PaymentService
    {
        public async Task<bool> ProcessCardPaymentAsync(decimal amount)
        {
            // Simulate connecting to a payment terminal and waiting for customer to swipe
            await Task.Delay(2000);
            return true; // Simulate success
        }
    }
}
