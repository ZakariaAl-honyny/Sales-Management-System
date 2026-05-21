using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for receiving logs from clients (Desktop app)
/// </summary>
[ApiController]
[Route("api/v1/logs")]
public class LogsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LogsController> _logger;

    public LogsController(IUnitOfWork uow, ILogger<LogsController> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous] // Allow logging even if not logged in (e.g., login errors)
    public async Task<IActionResult> Create([FromBody] CreateLogRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdStr, out var id) ? id : null;

        var log = SystemLog.Create(
            request.LogLevel,
            request.Message,
            request.Exception,
            request.StackTrace,
            request.Source ?? "Desktop",
            request.Context,
            userId,
            request.MachineName
        );

        await _uow.SystemLogs.AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct);

        // Also log to Serilog on server side for visibility
        if (request.LogLevel == "Error" || request.LogLevel == "Fatal")
        {
            _logger.LogError("Client Log [{Source}]: {Message} | Context: {Context} | Machine: {Machine}", 
                request.Source, request.Message, request.Context, request.MachineName);
        }
        else
        {
            _logger.LogWarning("Client Log [{Source}]: {Message} | Context: {Context} | Machine: {Machine}", 
                request.Source, request.Message, request.Context, request.MachineName);
        }

        return Ok();
    }
}
