using Microsoft.AspNetCore.SignalR;

namespace EnterprisePOS.Hubs
{
    /// <summary>
    /// SignalR hub that broadcasts notifications to ALL connected POS staff browsers.
    /// Used for online order alerts, unlike KitchenHub which targets the kitchen screen only.
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        public NotificationHub(ILogger<NotificationHub> logger) => _logger = logger;

        public override async Task OnConnectedAsync()
        {
            var tenantId = Context.User?.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Tenant_{tenantId}");
                _logger.LogInformation("Connection {ConnectionId} joined tenant group Tenant_{TenantId}", Context.ConnectionId, tenantId);
            }
            else
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Staff");
            }
            await base.OnConnectedAsync();
        }

        /// <summary>Called server-side to push a new online order alert to specific tenant staff.</summary>
        public async Task NotifyNewOnlineOrder(Guid tenantId, object orderData)
        {
            _logger.LogInformation("New online order notification broadcast to Tenant_{TenantId}.", tenantId);
            await Clients.Group($"Tenant_{tenantId}").SendAsync("NewOnlineOrder", orderData);
        }

        /// <summary>Called server-side to update order status for tracking page.</summary>
        public async Task UpdateOrderStatus(string orderNumber, string status)
        {
            _logger.LogInformation("Order {OrderNumber} status updated to {Status}", orderNumber, status);
            // Broadcast to customer tracking group
            await Clients.Group($"order_{orderNumber}").SendAsync("TrackingUpdate", status);
        }

        public async Task JoinOrderTracking(string orderNumber)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderNumber}");
        }
    }
}
