using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for dashboard statistics.
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IReportService reportService, ILogger<DashboardController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a high-level summary of store statistics for the dashboard.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dashboard summary data.</returns>
    [HttpGet("summary")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        _logger.LogInformation("Dashboard summary requested");
        var result = await _reportService.GetDashboardSummaryAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
