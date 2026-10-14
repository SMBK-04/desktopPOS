using Microsoft.AspNetCore.SignalR;

namespace EnterprisePOS.Hubs
{
    /// <summary>
    /// SignalR hub for real-time Kitchen Display System (KDS).
    /// Cashiers push new orders; kitchen screens receive them instantly.
    /// Kitchen staff can mark items as "ready" to notify cashier.
    /// </summary>
    public class KitchenHub : Hub
    {
        private readonly ILogger<KitchenHub> _logger;

        public KitchenHub(ILogger<KitchenHub> logger) => _logger = logger;

        /// <summary>Called by the POS when an order is completed — broadcasts to all kitchen screens.</summary>
        public async Task NewOrder(object orderData)
        {
            _logger.LogInformation("New kitchen order received via SignalR hub.");
            await Clients.Group("Kitchen").SendAsync("ReceiveOrder", orderData);
        }

        /// <summary>Called by kitchen staff to mark an item as prepared.</summary>
        public async Task MarkItemReady(string invoiceNumber, string itemName)
        {
            _logger.LogInformation("Item ready: {Item} on order {Invoice}", itemName, invoiceNumber);
            await Clients.Group("POS").SendAsync("ItemReady", invoiceNumber, itemName);
        }

        /// <summary>Called by kitchen staff to mark entire order complete.</summary>
        public async Task OrderComplete(string invoiceNumber)
        {
            _logger.LogInformation("Order complete: {Invoice}", invoiceNumber);
            await Clients.Group("POS").SendAsync("OrderComplete", invoiceNumber);
            await Clients.Group("Kitchen").SendAsync("OrderCleared", invoiceNumber);
        }

        public override async Task OnConnectedAsync()
        {
            var role = Context.GetHttpContext()?.Request.Query["role"].ToString();
            var tenantId = Context.User?.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                if (role == "kitchen")
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Kitchen_{tenantId}");
                else
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"POS_{tenantId}");
            }
            else
            {
                if (role == "kitchen")
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Kitchen");
                else
                    await Groups.AddToGroupAsync(Context.ConnectionId, "POS");
            }

            await base.OnConnectedAsync();
        }
    }
}
