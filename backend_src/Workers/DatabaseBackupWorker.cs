using EnterprisePOS.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.Workers
{
    /// <summary>
    /// Background hosted service that performs automated SQL Server database backups
    /// on a configurable schedule (default: daily at 2:00 AM).
    /// Backup files are stored in the /backups directory.
    /// </summary>
    public class DatabaseBackupWorker : BackgroundService
    {
        private readonly ILogger<DatabaseBackupWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly TimeSpan _interval;

        public DatabaseBackupWorker(
            ILogger<DatabaseBackupWorker> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration config)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _config = config;

            // Read interval from config (default: 24 hours)
            var hours = config.GetValue<int>("BackupSettings:IntervalHours", 24);
            _interval = TimeSpan.FromHours(hours);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Database Backup Worker started. Interval: {Interval}", _interval);

            // Wait until 2 AM for the first backup
            var now = TimeHelper.PKT;
            var nextRun = now.Date.AddDays(now.Hour >= 2 ? 1 : 0).AddHours(2);
            var delay = nextRun - now;

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await PerformBackupAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task PerformBackupAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var connectionString = db.Database.GetConnectionString()!;
                var builder = new SqlConnectionStringBuilder(connectionString);
                var dbName = builder.InitialCatalog;

                var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "backups");
                Directory.CreateDirectory(backupDir);

                var fileName = Path.Combine(backupDir, $"{dbName}_{TimeHelper.PKT:yyyyMMdd_HHmmss}.bak");

                var sql = $"BACKUP DATABASE [{dbName}] TO DISK = '{fileName}' WITH FORMAT, MEDIANAME = 'POSBackup', NAME = 'Full Database Backup';";

                await db.Database.ExecuteSqlRawAsync(sql, ct);

                _logger.LogInformation("Database backup completed: {File}", fileName);

                // Clean up backups older than 7 days
                CleanOldBackups(backupDir, 7);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database backup failed.");
            }
        }

        private void CleanOldBackups(string dir, int retentionDays)
        {
            var threshold = TimeHelper.PKT.AddDays(-retentionDays);
            foreach (var file in Directory.GetFiles(dir, "*.bak"))
            {
                if (File.GetCreationTime(file) < threshold)
                {
                    File.Delete(file);
                    _logger.LogInformation("Deleted old backup: {File}", file);
                }
            }
        }
    }
}
