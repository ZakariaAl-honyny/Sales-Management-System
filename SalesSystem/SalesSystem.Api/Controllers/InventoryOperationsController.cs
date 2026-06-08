using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Inventory operations management API (issuance, receipt, adjustment).
/// </summary>
[ApiController]
[Route("api/v1/inventory-operations")]
[Authorize(Policy = "ManagerAndAbove")]
public class InventoryOperationsController : ControllerBase
{
    private readonly IInventoryOperationService _inventoryOperationService;

    public InventoryOperationsController(IInventoryOperationService inventoryOperationService)
    {
        _inventoryOperationService = inventoryOperationService;
    }

    /// <summary>
    /// Gets a paginated list of inventory operations with optional filtering.
    /// </summary>
    /// <param name="warehouseId">Filter by warehouse ID (optional).</param>
    /// <param name="operationType">Filter by operation type — 1=Issue, 2=Receipt, 3=Adjustment (optional).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Items per page (default: 10).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InventoryOperationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? warehouseId,
        [FromQuery] byte? operationType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _inventoryOperationService.GetAllAsync(warehouseId, operationType, page, pageSize, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets an inventory operation by ID with its items.
    /// </summary>
    /// <param name="id">Operation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InventoryOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _inventoryOperationService.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
            return result.ErrorCode == ErrorCodes.NotFound
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new inventory operation (Draft status — no stock changes yet).
    /// </summary>
    /// <param name="request">The creation request with items.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(InventoryOperationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInventoryOperationRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _inventoryOperationService.CreateAsync(request, userId, ct);
        if (!result.IsSuccess)
            return result.ErrorCode == ErrorCodes.NotFound
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Posts (finalizes) a Draft inventory operation — stock changes take effect.
    /// </summary>
    /// <param name="id">Operation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{id:int}/post")]
    [ProducesResponseType(typeof(InventoryOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _inventoryOperationService.PostAsync(id, userId, ct);
        if (!result.IsSuccess)
            return result.ErrorCode == ErrorCodes.NotFound
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Cancels an inventory operation. Reverses stock changes if the operation was already posted.
    /// </summary>
    /// <param name="id">Operation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(InventoryOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _inventoryOperationService.CancelAsync(id, userId, ct);
        if (!result.IsSuccess)
            return result.ErrorCode == ErrorCodes.NotFound
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}
