using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using EnterprisePOS.ClientPOS.Data;
using EnterprisePOS.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EnterprisePOS.ClientPOS.Services
{
    public class BackgroundSyncService : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly DbContextOptions<LocalDbContext> _options;

        public BackgroundSyncService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5079") };
            _options = new DbContextOptionsBuilder<LocalDbContext>().UseSqlite("Data Source=localpos.db").Options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var context = new LocalDbContext(_options);

                    // 1. Sync Sales
                    var unsyncedSales = await context.Sales
                        .Include(s => s.SaleItems)
                        .Where(s => !s.IsSynced)
                        .ToListAsync(stoppingToken);

                    if (unsyncedSales.Any())
                    {
                        var response = await _httpClient.PostAsJsonAsync("/api/sync/sales", unsyncedSales, stoppingToken);
                        if (response.IsSuccessStatusCode)
                        {
                            foreach (var sale in unsyncedSales)
                            {
                                sale.IsSynced = true;
                            }
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }

                    // 2. Fetch License Status
                    var settings = await context.LocalSettings.FirstOrDefaultAsync(stoppingToken);
                    if (settings != null && !string.IsNullOrEmpty(settings.EncryptedLicenseToken))
                    {
                        var licResponse = await _httpClient.GetAsync($"/api/sync/license?token={settings.EncryptedLicenseToken}", stoppingToken);
                        if (licResponse.IsSuccessStatusCode)
                        {
                            var status = await licResponse.Content.ReadFromJsonAsync<LicenseStatusDto>(cancellationToken: stoppingToken);
                            if (status != null)
                            {
                                settings.IsPaymentDueBannerActive = status.IsPaymentDue;
                                settings.LastSyncTime = DateTime.UtcNow;
                                await context.SaveChangesAsync(stoppingToken);
                                
                                // Dispatch to UI thread if we had an event aggregator, but for now we rely on UI to poll or use INotifyPropertyChanged.
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore transient network errors, retry on next interval
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    public class LicenseStatusDto
    {
        public bool IsPaymentDue { get; set; }
        public bool IsActive { get; set; }
    }
}
