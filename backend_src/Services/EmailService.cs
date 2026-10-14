using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Services.Interfaces;

namespace EnterprisePOS.Services
{
    public class OfflineOrderItemDto
    {
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            return await SendMultipartEmailAsync(toEmail, subject, htmlBody, null, null);
        }

        public async Task<bool> SendMultipartEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody, string? displayName)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return false;

            try
            {
                var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.TryParse(_config["EmailSettings:SmtpPort"], out var p) ? p : 587;
                var senderEmail = _config["EmailSettings:SenderEmail"] ?? "enterprisepos1@gmail.com";
                var senderName = !string.IsNullOrEmpty(displayName) ? displayName : (_config["EmailSettings:SenderName"] ?? "Enterprise POS");
                var senderPassword = _config["EmailSettings:SenderPassword"];
                var enableSsl = !bool.TryParse(_config["EmailSettings:EnableSsl"], out var ssl) || ssl;

                if (string.IsNullOrWhiteSpace(senderPassword))
                {
                    _logger.LogInformation("SMTP password not configured. Simulated sending confirmation email to {ToEmail} for subject: {Subject}", toEmail, subject);
                    return true;
                }

                using var mail = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Priority = MailPriority.Normal,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };
                mail.To.Add(toEmail);
                mail.ReplyToList.Add(new MailAddress(senderEmail, senderName));

                // Anti-spam deliverability headers
                mail.Headers.Add("X-Mailer", "EnterprisePOS Transactional Emailer");
                mail.Headers.Add("X-Auto-Response-Suppress", "All");
                mail.Headers.Add("Precedence", "bulk");

                // Plain-text alternative view
                var plainText = !string.IsNullOrEmpty(plainTextBody) ? plainTextBody : "Your order confirmation details are attached.";
                var plainView = AlternateView.CreateAlternateViewFromString(plainText, Encoding.UTF8, MediaTypeNames.Text.Plain);
                mail.AlternateViews.Add(plainView);

                // HTML view
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html);
                mail.AlternateViews.Add(htmlView);

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000
                };

                await client.SendMailAsync(mail);
                _logger.LogInformation("Confirmation email successfully sent to {ToEmail} for {Subject}.", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}.", toEmail);
                return false;
            }
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(OnlineOrder order, string restaurantName)
        {
            if (string.IsNullOrWhiteSpace(order.CustomerEmail))
                return false;

            var storeName = !string.IsNullOrEmpty(restaurantName) ? restaurantName : "Restaurant";
            var subject = $"Order Confirmation #{order.OrderNumber} - {storeName}";

            var items = new List<OfflineOrderItemDto>();
            try
            {
                if (!string.IsNullOrEmpty(order.ItemsJson))
                {
                    items = JsonSerializer.Deserialize<List<OfflineOrderItemDto>>(order.ItemsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<OfflineOrderItemDto>();
                }
            }
            catch { }

            var itemsRows = new StringBuilder();
            var plainItems = new StringBuilder();

            foreach (var item in items)
            {
                var name = !string.IsNullOrEmpty(item.ProductName) ? item.ProductName : "Item";
                var qty = item.Quantity > 0 ? item.Quantity : 1;
                var price = item.UnitPrice;
                var total = item.LineTotal > 0 ? item.LineTotal : (qty * price);

                itemsRows.Append($@"
                    <tr style=""border-bottom: 1px solid #e2e8f0;"">
                        <td style=""padding: 12px 8px; font-weight: 500; color: #1e293b;"">{name} <span style=""color: #64748b; font-size: 13px;"">(x{qty})</span></td>
                        <td style=""padding: 12px 8px; text-align: right; font-weight: 600; color: #0f172a;"">Rs. {total:0.00}</td>
                    </tr>");

                plainItems.AppendLine($"- {name} (x{qty}): Rs. {total:0.00}");
            }

            var trackingUrl = $"https://enterpriseeat.runasp.net/Order/Track?orderNumber={Uri.EscapeDataString(order.OrderNumber)}";

            var plainBody = $@"
ORDER CONFIRMATION
========================================
Store: {storeName}
Order Number: {order.OrderNumber}
Order Date: {order.PlacedAt:dd MMM yyyy, hh:mm tt}
Order Type: {order.OrderType}
{(order.OrderType == OnlineOrderType.Delivery ? "Delivery Address: " + order.DeliveryAddress : "")}

ITEMS ORDERED:
{plainItems}

Total Amount: Rs. {order.TotalAmount:0.00}

Live Order Tracking:
{trackingUrl}

Thank you for your order!
{storeName} - Powered by Enterprise POS
";

            var htmlBody = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Order Confirmation #{order.OrderNumber}</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 24px; color: #334155; line-height: 1.5;"">
    <div style=""max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0; box-shadow: 0 2px 8px rgba(0,0,0,0.04);"">
        
        <!-- Header -->
        <div style=""background-color: #0f172a; padding: 28px 24px; text-align: center; color: #ffffff;"">
            <h2 style=""margin: 0 0 6px 0; font-size: 22px; font-weight: 700; color: #ffffff;"">{storeName}</h2>
            <p style=""margin: 0; font-size: 15px; color: #94a3b8;"">Order Confirmation</p>
        </div>

        <!-- Body -->
        <div style=""padding: 28px 24px;"">
            <p style=""font-size: 15px; margin: 0 0 20px 0;"">Hello,</p>
            <p style=""font-size: 14px; margin: 0 0 20px 0; color: #475569;"">Your order has been confirmed by {storeName} and is being prepared!</p>

            <div style=""background: #f8fafc; border-radius: 8px; padding: 16px; margin-bottom: 24px; border: 1px solid #e2e8f0;"">
                <table style=""width: 100%; border-collapse: collapse; font-size: 14px;"">
                    <tr>
                        <td style=""color: #64748b; padding-bottom: 6px;"">Order Number:</td>
                        <td style=""text-align: right; font-weight: 700; color: #0f172a; padding-bottom: 6px;"">{order.OrderNumber}</td>
                    </tr>
                    <tr>
                        <td style=""color: #64748b; padding-bottom: 6px;"">Order Type:</td>
                        <td style=""text-align: right; font-weight: 600; color: #0f172a; padding-bottom: 6px;"">{order.OrderType}</td>
                    </tr>
                    <tr>
                        <td style=""color: #64748b; padding-bottom: 6px;"">Date & Time:</td>
                        <td style=""text-align: right; color: #0f172a; padding-bottom: 6px;"">{order.PlacedAt:dd MMM yyyy, hh:mm tt}</td>
                    </tr>
                    {(order.OrderType == OnlineOrderType.Delivery && !string.IsNullOrEmpty(order.DeliveryAddress) ? $@"
                    <tr>
                        <td style=""color: #64748b;"">Delivery Address:</td>
                        <td style=""text-align: right; color: #0f172a;"">{order.DeliveryAddress}</td>
                    </tr>" : "")}
                </table>
            </div>

            <!-- Items Table -->
            <table style=""width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 14px;"">
                <thead>
                    <tr style=""border-bottom: 2px solid #e2e8f0; color: #64748b; font-size: 12px; text-transform: uppercase;"">
                        <th style=""text-align: left; padding: 8px;"">Item</th>
                        <th style=""text-align: right; padding: 8px;"">Amount</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsRows}
                </tbody>
                <tfoot>
                    <tr>
                        <td style=""padding: 14px 8px; font-size: 15px; font-weight: 700; color: #0f172a;"">Total</td>
                        <td style=""padding: 14px 8px; font-size: 16px; font-weight: 800; color: #0f172a; text-align: right;"">Rs. {order.TotalAmount:0.00}</td>
                    </tr>
                </tfoot>
            </table>

            <!-- Button -->
            <div style=""text-align: center; margin: 28px 0 16px 0;"">
                <a href=""{trackingUrl}"" style=""background-color: #2563eb; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 14px; display: inline-block;"">
                    Track Your Order Online
                </a>
            </div>
        </div>

        <!-- Footer -->
        <div style=""background: #f8fafc; border-top: 1px solid #e2e8f0; padding: 14px 24px; text-align: center; font-size: 12px; color: #94a3b8;"">
            &copy; {DateTime.UtcNow.Year} {storeName}. All rights reserved.
        </div>
    </div>
</body>
</html>";

            return await SendMultipartEmailAsync(order.CustomerEmail, subject, htmlBody, plainBody, storeName);
        }
    }
}
