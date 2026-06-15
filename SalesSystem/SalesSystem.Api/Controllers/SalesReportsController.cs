using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for sales-related reports (Phase 31).
/// All endpoints require Manager or Admin role.
/// </summary>
[ApiController]
[Route("api/v1/reports/sales")]
[Authorize(Policy = "ManagerAndAbove")]
public class SalesReportsController : ControllerBase
{
    private readonly ISalesReportService _salesReportService;

    public SalesReportsController(ISalesReportService salesReportService)
    {
        _salesReportService = salesReportService;
    }

    /// <summary>
    /// Gets sales grouped by customer.
    /// </summary>
    [HttpGet("by-customer")]
    public async Task<IActionResult> GetSalesByCustomer([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _salesReportService.GetSalesByCustomerAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets sales grouped by product with profit analysis.
    /// </summary>
    [HttpGet("by-product")]
    public async Task<IActionResult> GetSalesByProduct([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _salesReportService.GetSalesByProductAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets sales grouped by product category.
    /// </summary>
    [HttpGet("by-category")]
    public async Task<IActionResult> GetSalesByCategory([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _salesReportService.GetSalesByCategoryAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets daily sales summary for a date range.
    /// </summary>
    [HttpGet("daily-summary")]
    public async Task<IActionResult> GetDailySalesSummary([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _salesReportService.GetDailySalesSummaryAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets sales trends grouped by period (monthly, quarterly, yearly).
    /// </summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetSalesTrends([FromQuery] DateTime from, [FromQuery] DateTime to,
        CancellationToken ct, [FromQuery] string groupBy = "monthly")
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _salesReportService.GetSalesTrendsAsync(from, to, groupBy, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets sales grouped by user (who created the invoice).
    /// </summary>
    [HttpGet("by-user")]
    public async Task<IActionResult> GetSalesByUser([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _salesReportService.GetSalesByUserAsync(from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
