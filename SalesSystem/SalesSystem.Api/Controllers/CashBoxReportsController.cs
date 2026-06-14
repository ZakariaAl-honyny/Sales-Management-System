using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for cash box-related reports.
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
    /// Gets cash box summary (balance info per cash box).
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetCashBoxSummary([FromQuery] DateTime? asOfDate, CancellationToken ct)
    {
        var result = await _cashBoxReportService.GetCashBoxSummaryAsync(asOfDate, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets receipt vouchers for a specific period.
    /// </summary>
    [HttpGet("receipt-vouchers")]
    public async Task<IActionResult> GetReceiptVoucherReport([FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] int? cashBoxId, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _cashBoxReportService.GetReceiptVoucherReportAsync(from, to, cashBoxId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets payment vouchers for a specific period.
    /// </summary>
    [HttpGet("payment-vouchers")]
    public async Task<IActionResult> GetPaymentVoucherReport([FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] int? cashBoxId, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _cashBoxReportService.GetPaymentVoucherReportAsync(from, to, cashBoxId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
