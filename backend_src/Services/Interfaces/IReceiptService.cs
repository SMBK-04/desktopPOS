using EnterprisePOS.Models.Entities;

namespace EnterprisePOS.Services.Interfaces
{
    public interface IReceiptService
    {
        /// <summary>Generates a full Cashier Receipt PDF (prices, totals, invoice).</summary>
        byte[] GenerateCashierReceipt(Sale sale);

        /// <summary>Generates a Kitchen Receipt PDF (items + qty only, no pricing).</summary>
        byte[] GenerateKitchenReceipt(Sale sale);
    }
}
