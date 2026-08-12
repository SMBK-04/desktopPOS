using System.Windows;
using System.Threading.Tasks;
using EnterprisePOS.ServerAPI.Data;
using EnterprisePOS.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterprisePOS.SuperAdmin;

public partial class App : Application
{
    private WebApplication? _apiServer;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var builder = WebApplication.CreateBuilder();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                })
                .AddApplicationPart(typeof(EnterprisePOS.ServerAPI.Controllers.TenantsController).Assembly);
            builder.Services.AddEndpointsApiExplorer();

            var connString = "Server=(localdb)\\mssqllocaldb;Database=EnterprisePOS_Central;Trusted_Connection=True;MultipleActiveResultSets=true";
            builder.Services.AddDbContext<CentralDbContext>(options =>
                options.UseSqlServer(connString));

            builder.Services.AddSingleton<LicenseService>();

            // Set the listening URL
            builder.WebHost.UseKestrel(options =>
            {
                options.ListenLocalhost(5079);
            });

            _apiServer = builder.Build();

            using (var scope = _apiServer.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CentralDbContext>();
                await db.Database.EnsureCreatedAsync();
            }

            _apiServer.UseAuthorization();
            _apiServer.MapControllers();

            // Run API in background so WPF UI is not blocked
            _ = Task.Run(() => _apiServer.Run());
        }
        catch
        {
            // If API fails to start, Super Admin still opens
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_apiServer != null)
        {
            await _apiServer.StopAsync();
            _apiServer = null;
        }
        base.OnExit(e);
    }
}
