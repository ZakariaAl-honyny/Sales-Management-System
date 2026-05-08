using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Inventory;
using SalesSystem.Contracts.DTOs;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Stock transfers management API
/// </summary>
[ApiController]
[Route("api/v1/stock-transfers")]
[Authorize(Policy = "ManagerAndAbove")]
public class StockTransfersController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public StockTransfersController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Gets all stock transfers with optional filtering and pagination
    /// </summary>
    /// <param name="fromWarehouseId">Filter by source warehouse ID</param>
    /// <param name="toWarehouseId">Filter by destination warehouse ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of stock transfers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StockTransferDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? fromWarehouseId, 
        [FromQuery] int? toWarehouseId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken ct = default)
    {
        var result = await _inventoryService.GetAllTransfersAsync(fromWarehouseId, toWarehouseId, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a stock transfer by ID
    /// </summary>
    /// <param name="id">Transfer ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Transfer details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StockTransferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _inventoryService.GetTransferByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new stock transfer (atomically moves stock between warehouses)
    /// </summary>
    /// <param name="request">Stock transfer creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created transfer</returns>
    [HttpPost]
    [ProducesResponseType(typeof(StockTransferDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStockTransferRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _inventoryService.CreateTransferAsync(request, userId, ct);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) 
            : BadRequest(new { error = result.Error });
    }
}