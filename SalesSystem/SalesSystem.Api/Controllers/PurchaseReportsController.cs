using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for purchase-related reports (Phase 31).
/// All endpoints require Manager or Admin role.
/// </summary>
[ApiController]
[Route("api/v1/reports/purchases")]
[Authorize(Policy = "ManagerAndAbove")]
public class PurchaseReportsController : ControllerBase
{
    private readonly IPurchaseReportService _purchaseReportService;

    public PurchaseReportsController(IPurchaseReportService purchaseReportService)
    {
        _purchaseReportService = purchaseReportService;
    }

    /// <summary>
    /// Gets purchases grouped by supplier.
    /// </summary>
    [HttpGet("by-supplier")]
    public async Task<IActionResult> GetPurchasesBySupplier([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _purchaseReportService.GetPurchasesBySupplierAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets purchases grouped by product with cost analysis.
    /// </summary>
    [HttpGet("by-product")]
    public async Task<IActionResult> GetPurchasesByProduct([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _purchaseReportService.GetPurchasesByProductAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets purchase trends grouped by period (monthly, quarterly, yearly).
    /// </summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetPurchaseTrends([FromQuery] DateTime from, [FromQuery] DateTime to,
        CancellationToken ct, [FromQuery] string groupBy = "monthly")
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _purchaseReportService.GetPurchaseTrendsAsync(from, to, groupBy, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
