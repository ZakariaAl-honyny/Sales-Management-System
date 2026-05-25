using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Health check endpoints for verifying application health and database connection.
/// </summary>
[ApiController]
[Route("api/v1/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthChecker;
    private readonly ILogger<HealthController> _logger;

    public HealthController(HealthCheckService healthChecker, ILogger<HealthController> logger)
    {
        _healthChecker = healthChecker;
        _logger = logger;
    }

    /// <summary>
    /// Gets overall system health including database connectivity.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        try
        {
            var report = await _healthChecker.CheckHealthAsync(ct);
            if (report.Status == HealthStatus.Healthy)
            {
                return Ok(new HealthCheckDto("Healthy", "Connected", DateTime.UtcNow));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed");
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new HealthCheckDto("Unhealthy", "Disconnected", DateTime.UtcNow));
    }

    /// <summary>
    /// Gets database connectivity state only.
    /// </summary>
    [HttpGet("database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDatabaseHealth(CancellationToken ct)
    {
        try
        {
            var report = await _healthChecker.CheckHealthAsync(ct);

            if (report.Entries.TryGetValue("database", out var dbEntry) &&
                dbEntry.Status == HealthStatus.Healthy)
            {
                return Ok(new { status = "connected" });
            }

            if (report.Status == HealthStatus.Healthy)
            {
                return Ok(new { status = "connected" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { status = "disconnected" });
    }
}
