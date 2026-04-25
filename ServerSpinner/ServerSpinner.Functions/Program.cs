using Microsoft.Azure.SignalR.Management;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServerSpinner.Functions.Contracts;
using ServerSpinner.Functions.Data;
using ServerSpinner.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["DbConnectionString"]
                               ?? throw new InvalidOperationException("DbConnectionString not configured");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHttpClient();

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var signalRConnection = config["AzureSignalRConnectionString"]
                                    ?? throw new InvalidOperationException(
                                        "AzureSignalRConnectionString not configured");
            return new ServiceManagerBuilder()
                .WithOptions(o => o.ConnectionString = signalRConnection)
                .BuildServiceManager();
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISettingsMapper, SettingsMapper>();
        services.AddScoped<ISpinnerConfigMapper, SpinnerConfigMapper>();
        services.AddScoped<ISpinnerStateService, SpinnerStateService>();
        services.AddScoped<IAutoPlayService, AutoPlayService>();
    })
    .Build();

await host.RunAsync();