using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SalesSystem.Infrastructure.Data;
using System.Threading.Tasks;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Health check endpoints for verifying application health and database connection.
/// </summary>
[ApiController]
[Route("api/v1/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly SalesDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(SalesDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets overall system health including database connectivity.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var isConnected = await _dbContext.Database.CanConnectAsync();
            if (isConnected)
            {
                return Ok(new { status = "healthy", database = "connected" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed: database unreachable");
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy", database = "disconnected" });
    }

    /// <summary>
    /// Gets database connectivity state only.
    /// </summary>
    [HttpGet("database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        try
        {
            var isConnected = await _dbContext.Database.CanConnectAsync();
            if (isConnected)
            {
                return Ok(new { status = "connected" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed: database unreachable");
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "disconnected" });
    }
}
