using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Inventory management API (stock levels and movements)
/// </summary>
[ApiController]
[Route("api/v1/inventory")]
[Authorize(Policy = "ManagerAndAbove")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Gets stock quantity for a specific product in a warehouse
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Stock quantity</returns>
    [HttpGet("stock")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStock([FromQuery] int productId, [FromQuery] int warehouseId, CancellationToken ct)
    {
        var result = await _inventoryService.GetStockAsync(productId, warehouseId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets all inventory movements with optional filtering and pagination
    /// </summary>
    /// <param name="productId">Filter by product ID</param>
    /// <param name="warehouseId">Filter by warehouse ID</param>
    /// <param name="movementType">Filter by movement type</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of inventory movements</returns>
    [HttpGet("movements")]
    [ProducesResponseType(typeof(PagedResult<InventoryMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMovements(
        [FromQuery] int? productId,
        [FromQuery] int? warehouseId,
        [FromQuery] int? movementType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _inventoryService.GetMovementsAsync(productId, warehouseId, movementType, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets all warehouse stocks with optional filtering
    /// </summary>
    /// <param name="warehouseId">Filter by warehouse ID</param>
    /// <param name="productId">Filter by product ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of warehouse stocks</returns>
    [HttpGet("warehouse-stocks")]
    [ProducesResponseType(typeof(PagedResult<WarehouseStockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWarehouseStocks(
        [FromQuery] int? warehouseId,
        [FromQuery] int? productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _inventoryService.GetWarehouseStocksAsync(warehouseId, productId, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}