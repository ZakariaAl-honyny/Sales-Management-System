using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/customer-receipts")]
[Authorize]
public class CustomerReceiptsController : ControllerBase
{
    private readonly ICustomerReceiptService _service;

    public CustomerReceiptsController(ICustomerReceiptService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all customer receipts.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a customer receipt by ID with its applications.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new customer receipt (Draft status).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerReceiptRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.CreateAsync(request, userId, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Posts (finalizes) a customer receipt — cash box and customer balance are updated.
    /// </summary>
    [HttpPost("{id:int}/post")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.PostAsync(id, userId, ct);
        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Cancels a customer receipt. Reverses cash box and balance changes if already posted.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.CancelAsync(id, userId, ct);
        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Adds an invoice application to a customer receipt (allocates amount to a specific invoice).
    /// </summary>
    [HttpPost("{id:int}/applications")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> AddApplication(int id, [FromBody] AddReceiptApplicationRequest request, CancellationToken ct)
    {
        var result = await _service.AddApplicationAsync(id, request, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
