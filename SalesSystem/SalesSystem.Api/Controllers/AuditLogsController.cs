using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for querying audit logs and user activity history.
/// Access restricted to Admin role only (Policy: AdminOnly).
/// </summary>
[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Policy = "AdminOnly")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Queries audit logs with optional filters and pagination.
    /// </summary>
    /// <param name="query">Query parameters including UserId, Action, EntityType, date range, and pagination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns paginated audit log entries.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Query([FromQuery] AuditLogQuery query, CancellationToken ct)
    {
        var result = await _auditLogService.QueryAsync(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets the recent audit history for a specific user.
    /// </summary>
    /// <param name="userId">User ID to get history for.</param>
    /// <param name="limit">Maximum number of entries to return (default 50).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns list of audit log entries for the specified user.</returns>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserHistory(int userId, CancellationToken ct, [FromQuery] int limit = 50)
    {
        var result = await _auditLogService.GetUserHistoryAsync(userId, limit, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets the recent login history, optionally filtered by user.
    /// </summary>
    /// <param name="userId">Optional user ID to filter by.</param>
    /// <param name="limit">Maximum number of entries to return (default 50).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns list of login audit log entries.</returns>
    [HttpGet("login-history")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLoginHistory([FromQuery] int? userId, CancellationToken ct, [FromQuery] int limit = 50)
    {
        var result = await _auditLogService.GetLoginHistoryAsync(userId, limit, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
