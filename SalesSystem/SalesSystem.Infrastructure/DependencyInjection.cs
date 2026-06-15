using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Updates;
using SalesSystem.Infrastructure.Repositories;
using SalesSystem.Infrastructure.Services;
using SalesSystem.Infrastructure.Updates;

namespace SalesSystem.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure-layer services including image storage,
    /// updater, and future cross-cutting services.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISystemLogRepository, SystemLogRepository>();

        // LocalImageStorageService holds no scoped state (_basePath is static, ILogger is singleton)
        services.AddSingleton<ILocalImageStorageService, LocalImageStorageService>();

        return services;
    }

    public static IServiceCollection AddUpdateServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Default updater: checks a custom version.json endpoint (configured via UpdateCheckUrl).
        // Alternative: GitHubUpdaterService checks GitHub releases instead.
        // To switch, replace UpdaterService with GitHubUpdaterService and configure GitHub:Owner + GitHub:Repository.
        services.AddHttpClient<IUpdaterService, UpdaterService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "SalesSystem-UpdateChecker/1.0");
            client.DefaultRequestHeaders.CacheControl =
                new System.Net.Http.Headers.CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true
                };
        });

        return services;
    }
}
