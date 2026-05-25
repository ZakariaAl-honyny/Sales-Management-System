using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace SalesSystem.Infrastructure.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IConfiguration configuration, ILogger<DatabaseHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            var connString = _configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("SALESSYSTEM_DB_CONNECTION");

            if (string.IsNullOrEmpty(connString))
                return HealthCheckResult.Unhealthy("Unreachable");

            var builder = new SqlConnectionStringBuilder(connString)
            {
                ConnectTimeout = 3
            };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 3;
            await command.ExecuteScalarAsync(ct);

            return HealthCheckResult.Healthy("Reachable");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Unreachable", exception: ex);
        }
    }
}
