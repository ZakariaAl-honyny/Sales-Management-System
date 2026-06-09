using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for cash box-related reports (Phase 31).
/// All endpoints require Manager or Admin role.
/// </summary>
[ApiController]
[Route("api/v1/reports/cash-boxes")]
[Authorize(Policy = "ManagerAndAbove")]
public class CashBoxReportsController : ControllerBase
{
    private readonly ICashBoxReportService _cashBoxReportService;

    public CashBoxReportsController(ICashBoxReportService cashBoxReportService)
    {
        _cashBoxReportService = cashBoxReportService;
    }

    /// <summary>
    /// Gets cash box summary (opening, income, expense, closing) for each cash box.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetCashBoxSummary([FromQuery] DateTime? asOfDate, CancellationToken ct)
    {
        var result = await _cashBoxReportService.GetCashBoxSummaryAsync(asOfDate, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets daily closure report for cash boxes.
    /// </summary>
    [HttpGet("daily-closures")]
    public async Task<IActionResult> GetDailyClosureReport([FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] int? cashBoxId, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _cashBoxReportService.GetDailyClosureReportAsync(from, to, cashBoxId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets transaction details for a specific cash box.
    /// </summary>
    [HttpGet("transaction-detail/{cashBoxId:int:min(1)}")]
    public async Task<IActionResult> GetCashTransactionDetails(int cashBoxId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _cashBoxReportService.GetCashTransactionDetailsAsync(cashBoxId, from, to, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "NotFound" ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }
}
