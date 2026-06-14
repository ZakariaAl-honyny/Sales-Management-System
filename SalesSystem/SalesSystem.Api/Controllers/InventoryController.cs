using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

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
    [ProducesResponseType(typeof(PagedResult<InventoryTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMovements(
        [FromQuery] int? productId,
        [FromQuery] int? warehouseId,
        [FromQuery] int? transactionType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _inventoryService.GetMovementsAsync(productId, warehouseId, transactionType, page, pageSize, ct);
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

    /// <summary>
    /// Creates a new inventory transaction
    /// </summary>
    [HttpPost("transactions")]
    [ProducesResponseType(typeof(InventoryTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateInventoryTransactionRequest request, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await _inventoryService.CreateTransactionAsync(request, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Posts (approves) an inventory transaction — affects stock
    /// </summary>
    [HttpPost("transactions/{id}/post")]
    [ProducesResponseType(typeof(InventoryTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostTransaction(int id, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await _inventoryService.PostTransactionAsync(id, userId, ct);
        if (!result.IsSuccess)
            return result.Error == ErrorCodes.NotFound ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Cancels an inventory transaction — reverses stock
    /// </summary>
    [HttpPost("transactions/{id}/cancel")]
    [ProducesResponseType(typeof(InventoryTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelTransaction(int id, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await _inventoryService.CancelTransactionAsync(id, userId, ct);
        if (!result.IsSuccess)
            return result.Error == ErrorCodes.NotFound ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets an inventory transaction by ID
    /// </summary>
    [HttpGet("transactions/{id}")]
    [ProducesResponseType(typeof(InventoryTransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(int id, CancellationToken ct)
    {
        var result = await _inventoryService.GetTransactionByIdAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }
}