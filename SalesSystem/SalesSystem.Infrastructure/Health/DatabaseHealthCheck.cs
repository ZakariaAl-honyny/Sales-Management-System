using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using SalesSystem.Infrastructure.Persistence;

namespace SalesSystem.Infrastructure.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly SecureDbContextFactory _dbFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(SecureDbContextFactory dbFactory,
        ILogger<DatabaseHealthCheck> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            var connString = _dbFactory.GetDecryptedConnectionString();

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
        catch (InvalidOperationException ex) when (ex.Message.Contains("Connection string", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex, "Database connection string not configured");
            return HealthCheckResult.Unhealthy("Connection string not configured");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Unreachable", exception: ex);
        }
    }
}
