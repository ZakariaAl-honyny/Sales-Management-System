using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Sales Quotations management API
/// Quotations are non-binding price quotes with NO stock/accounting impact.
/// </summary>
[ApiController]
[Route("api/v1/sales-quotations")]
public class SalesQuotationsController : ControllerBase
{
    private readonly ISalesQuotationService _quotationService;

    public SalesQuotationsController(ISalesQuotationService quotationService)
    {
        _quotationService = quotationService;
    }

    /// <summary>
    /// Gets all sales quotations with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(PagedResult<SalesQuotationDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] int? status,
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _quotationService.GetAllAsync(customerId, status, search, from, to, page, pageSize, includeInactive, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a sales quotation by ID with full details and items
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _quotationService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new sales quotation (draft)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateSalesQuotationRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _quotationService.CreateAsync(request, userId, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing sales quotation (draft only)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSalesQuotationRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _quotationService.UpdateAsync(id, request, userId, ct);
        if (!result.IsSuccess && result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Sends a draft quotation to the customer (Draft → Sent)
    /// </summary>
    [HttpPost("{id:int}/send")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Send(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _quotationService.SendAsync(id, userId, ct);
        if (!result.IsSuccess && result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Accepts a sent quotation (Sent → Accepted)
    /// </summary>
    [HttpPost("{id:int}/accept")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Accept(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _quotationService.AcceptAsync(id, userId, ct);
        if (!result.IsSuccess && result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Rejects a sent/accepted quotation with optional reason
    /// </summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectSalesQuotationRequest? request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _quotationService.RejectAsync(id, request?.Reason, userId, ct);
        if (!result.IsSuccess && result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Converts an accepted/sent quotation to a sales invoice
    /// </summary>
    [HttpPost("{id:int}/convert")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SalesQuotationDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ConvertToInvoice(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _quotationService.ConvertToInvoiceAsync(id, userId, ct);
        if (!result.IsSuccess && result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Cancels/rejects a quotation (non-terminal → Rejected)
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _quotationService.CancelAsync(id, userId, ct);
        if (!result.IsSuccess && result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return result.IsSuccess ? Ok(new { message = "تم إلغاء عرض السعر بنجاح" }) : BadRequest(new { error = result.Error });
    }
}
