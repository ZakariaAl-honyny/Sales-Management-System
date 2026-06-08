using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Purchase orders management API
/// </summary>
[ApiController]
[Route("api/v1/purchase-orders")]
[Authorize(Policy = "ManagerAndAbove")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;

    public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
    }

    /// <summary>
    /// Gets all purchase orders with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PurchaseOrderDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? supplierId,
        [FromQuery] int? status,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await _purchaseOrderService.GetAllAsync(supplierId, status, search, from, to, page, pageSize, includeInactive, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a purchase order by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _purchaseOrderService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new purchase order (Draft)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PurchaseOrderDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseOrderService.CreateAsync(request, userId, ct);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates a purchase order (Draft only)
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePurchaseOrderRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseOrderService.UpdateAsync(id, request, userId, ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Cancels a purchase order (Draft, Approved or PartiallyReceived orders)
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseOrderService.CancelAsync(id, userId, ct);

        if (result.IsSuccess)
            return Ok();

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets pending purchase orders (not yet received or cancelled)
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<PurchaseOrderDto>), 200)]
    public async Task<IActionResult> GetPendingOrders(CancellationToken ct)
    {
        var result = await _purchaseOrderService.GetPendingOrdersAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
