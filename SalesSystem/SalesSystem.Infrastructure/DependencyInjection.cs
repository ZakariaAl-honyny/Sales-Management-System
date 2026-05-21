using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesSystem.Application.Updates;
using SalesSystem.Infrastructure.Updates;

namespace SalesSystem.Infrastructure;

public static class DependencyInjection
{
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
