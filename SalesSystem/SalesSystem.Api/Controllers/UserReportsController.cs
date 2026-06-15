using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for user-related reports (Phase 31).
/// All endpoints require Admin role due to sensitive user data.
/// </summary>
[ApiController]
[Route("api/v1/reports/users")]
[Authorize(Policy = "AdminOnly")]
public class UserReportsController : ControllerBase
{
    private readonly IUserReportService _userReportService;

    public UserReportsController(IUserReportService userReportService)
    {
        _userReportService = userReportService;
    }

    /// <summary>
    /// Gets user activity log within a date range.
    /// </summary>
    [HttpGet("activity")]
    public async Task<IActionResult> GetUserActivity([FromQuery] int? userId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _userReportService.GetUserActivityAsync(userId, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets login history within a date range.
    /// </summary>
    [HttpGet("login-history")]
    public async Task<IActionResult> GetLoginHistory([FromQuery] int? userId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _userReportService.GetLoginHistoryAsync(userId, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets audit trail summary within a date range.
    /// </summary>
    [HttpGet("audit-trail")]
    public async Task<IActionResult> GetAuditTrailSummary([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _userReportService.GetAuditTrailSummaryAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
