using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for generating various reports.
/// </summary>
/// <remarks>
/// All report endpoints require Manager or Admin role (Policy: ManagerAndAbove)
/// </remarks>
[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Generates sales report for a date range.
    /// </summary>
    [HttpGet("sales")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetSalesReport([FromQuery] int? warehouseId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _reportService.GetSalesReportAsync(warehouseId, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Generates purchases report for a date range.
    /// </summary>
    [HttpGet("purchases")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetPurchasesReport([FromQuery] int? warehouseId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { error = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية" });

        var result = await _reportService.GetPurchasesReportAsync(warehouseId, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Generates stock report for a specific warehouse or all warehouses.
    /// </summary>
    [HttpGet("stock")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetStockReport([FromQuery] int? warehouseId, CancellationToken ct)
    {
        var result = await _reportService.GetStockReportAsync(warehouseId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Generates customer balances report.
    /// </summary>
    [HttpGet("customers")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetCustomerBalancesReport([FromQuery] int? customerId, CancellationToken ct)
    {
        var result = await _reportService.GetCustomerBalancesReportAsync(customerId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Generates supplier balances report.
    /// </summary>
    [HttpGet("suppliers")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetSupplierBalancesReport([FromQuery] int? supplierId, CancellationToken ct)
    {
        var result = await _reportService.GetSupplierBalancesReportAsync(supplierId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Generates product movements report for a specific product.
    /// </summary>
    [HttpGet("product-movements")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetProductMovementsReport([FromQuery] int productId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        if (productId <= 0)
            return BadRequest(new { error = "معرف المنتج مطلوب" });

        var result = await _reportService.GetProductMovementsReportAsync(productId, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Generates low stock report (products below reorder level).
    /// </summary>
    [HttpGet("low-stock")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetLowStockReport([FromQuery] int? warehouseId, CancellationToken ct)
    {
        var result = await _reportService.GetLowStockReportAsync(warehouseId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Generates stock balance report — shows current stock, reorder level, average cost, and total value per product/warehouse.
    /// </summary>
    [HttpGet("stock-balance")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetStockBalanceReport([FromQuery] int? warehouseId, CancellationToken ct)
    {
        var result = await _reportService.GetStockBalanceReportAsync(warehouseId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Generates warehouse movements report — shows inventory movements with quantities before/after.
    /// </summary>
    [HttpGet("warehouse-movements")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetWarehouseMovementsReport([FromQuery] int? warehouseId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await _reportService.GetWarehouseMovementsAsync(warehouseId, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets list of expired or expiring products within the given threshold days.
    /// thresholdDays = 0 returns only already-expired products.
    /// </summary>
    [HttpGet("expired-products")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(IEnumerable<Contracts.DTOs.ExpiredProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetExpiredProducts(CancellationToken ct, [FromQuery] int thresholdDays = 0)
    {
        var result = await _reportService.GetExpiredProductsReportAsync(thresholdDays, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
