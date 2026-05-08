using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Sales;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Sales invoices management API
/// </summary>
[ApiController]
[Route("api/v1/sales-invoices")]
[Authorize(Policy = "AllStaff")]
public class SalesInvoicesController : ControllerBase
{
    private readonly ISalesService _salesService;

    public SalesInvoicesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    /// <summary>
    /// Gets all sales invoices with optional filtering and pagination
    /// </summary>
    /// <param name="customerId">Filter by customer ID</param>
    /// <param name="status">Filter by status (1=Draft, 2=Posted, 3=Cancelled)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of sales invoices</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SalesInvoiceDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? customerId, 
        [FromQuery] int? status, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken ct = default)
    {
        var result = await _salesService.GetAllAsync(customerId, status, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a sales invoice by ID
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Invoice details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SalesInvoiceDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _salesService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new sales invoice (draft)
    /// </summary>
    /// <param name="request">Sales invoice creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created invoice</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SalesInvoiceDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateSalesInvoiceRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _salesService.CreateAsync(request, userId, ct);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Posts a sales invoice (validates stock and updates balances)
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Posted invoice</returns>
    [HttpPost("{id:int}/post")]
    [ProducesResponseType(typeof(SalesInvoiceDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _salesService.PostAsync(id, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Cancels a sales invoice (reverses stock and balances)
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cancelled invoice</returns>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(SalesInvoiceDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _salesService.CancelAsync(id, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}