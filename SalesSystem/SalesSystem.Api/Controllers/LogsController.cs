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
}
