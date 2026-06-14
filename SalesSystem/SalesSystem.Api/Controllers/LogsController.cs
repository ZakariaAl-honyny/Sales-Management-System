using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for receiving logs from clients (Desktop app).
/// Delegates all data access to ILogService — no direct IUnitOfWork dependency.
/// </summary>
[ApiController]
[Route("api/v1/logs")]
[Authorize(Policy = "AllStaff")]
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogService logService, ILogger<LogsController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLogRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdStr, out var id) ? id : null;

        var result = await _logService.CreateLogAsync(
            request.LogLevel,
            request.Message,
            request.Exception,
            request.StackTrace,
            request.Source ?? "Desktop",
            request.Context,
            userId,
            request.MachineName,
            ct);

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to store client log: {Error}", result.Error);
            return StatusCode(500, new { error = result.Error });
        }

        return Ok();
    }

    /// <summary>
    /// Queries system logs with optional filtering and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? level,
        [FromQuery] string? source,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _logService.QueryLogsAsync(level, source, search, from, to, page, pageSize, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        return BadRequest(new { error = result.Error });
    }
}
