using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Warehouse transfers (تحويلات مخزنية) management API
/// </summary>
[ApiController]
[Route("api/v1/warehouse-transfers")]
[Authorize(Policy = "ManagerAndAbove")]
public class WarehouseTransfersController : ControllerBase
{
    private readonly IWarehouseTransferService _warehouseTransferService;

    public WarehouseTransfersController(IWarehouseTransferService warehouseTransferService)
    {
        _warehouseTransferService = warehouseTransferService;
    }

    /// <summary>
    /// Gets all warehouse transfers with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WarehouseTransferDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? sourceWarehouseId,
        [FromQuery] int? destinationWarehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _warehouseTransferService.GetAllAsync(sourceWarehouseId, destinationWarehouseId, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a warehouse transfer by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WarehouseTransferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _warehouseTransferService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new warehouse transfer
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WarehouseTransferDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseTransferRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _warehouseTransferService.CreateAsync(request, userId, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Posts (confirms) a warehouse transfer — moves stock and records inventory transaction
    /// </summary>
    [HttpPost("{id:int}/post")]
    [ProducesResponseType(typeof(WarehouseTransferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _warehouseTransferService.PostAsync(id, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Cancels a warehouse transfer — reverses stock movements if it was posted
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(WarehouseTransferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _warehouseTransferService.CancelAsync(id, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
