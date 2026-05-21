using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Purchase invoices management API
/// </summary>
[ApiController]
[Route("api/v1/purchase-invoices")]
[Authorize(Policy = "ManagerAndAbove")]
public class PurchaseInvoicesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchaseInvoicesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    /// <summary>
    /// Gets all purchase invoices with optional filtering and pagination
    /// </summary>
    /// <param name="supplierId">Filter by supplier ID</param>
    /// <param name="status">Filter by status (1=Draft, 2=Posted, 3=Cancelled)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of purchase invoices</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PurchaseInvoiceDto>), 200)]
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
        var result = await _purchaseService.GetAllAsync(supplierId, status, search, from, to, page, pageSize, includeInactive, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a purchase invoice by ID
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Invoice details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _purchaseService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Gets a purchase invoice by number
    /// </summary>
    /// <param name="invoiceNo">Invoice number</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Invoice details</returns>
    [HttpGet("number/{invoiceNo}")]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByNumber(string invoiceNo, CancellationToken ct)
    {
        var result = await _purchaseService.GetByNumberAsync(invoiceNo, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new purchase invoice (draft)
    /// </summary>
    /// <param name="request">Purchase invoice creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created invoice</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseInvoiceRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseService.CreateAsync(request, userId, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing purchase invoice (draft only)
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="request">Purchase invoice update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated invoice</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePurchaseInvoiceRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseService.UpdateAsync(id, request, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Posts a purchase invoice (adds stock and updates supplier balance)
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Posted invoice</returns>
    [HttpPost("{id:int}/post")]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseService.PostAsync(id, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Cancels a purchase invoice (reverses stock and balances)
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cancelled invoice</returns>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(PurchaseInvoiceDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _purchaseService.CancelAsync(id, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}