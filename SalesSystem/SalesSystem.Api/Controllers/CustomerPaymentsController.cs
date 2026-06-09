using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Customer payments management API
/// </summary>
[ApiController]
[Route("api/v1/customer-payments")]
[Authorize]
public class CustomerPaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IChequeService _chequeService;

    public CustomerPaymentsController(
        IPaymentService paymentService,
        IChequeService chequeService)
    {
        _paymentService = paymentService;
        _chequeService = chequeService;
    }

    /// <summary>
    /// Creates a customer payment (reduces customer balance)
    /// </summary>
    /// <param name="request">Customer payment creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created payment</returns>
    [HttpPost]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(CustomerPaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerPaymentRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _paymentService.CreateCustomerPaymentAsync(request, userId, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets all customer payments with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(PagedResult<CustomerPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _paymentService.GetCustomerPaymentsAsync(search, from, to, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a customer payment by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(CustomerPaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _paymentService.GetCustomerPaymentByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Updates a customer payment and adjusts balance
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(CustomerPaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerPaymentRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _paymentService.UpdateCustomerPaymentAsync(id, request, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.Error == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a customer payment and reverses balance impact
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _paymentService.DeleteCustomerPaymentAsync(id, userId, ct);
        if (result.IsSuccess) return NoContent();
        if (result.Error == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Creates a cheque linked to a customer payment (e.g., when PaymentMethod = Cheque).
    /// </summary>
    [HttpPost("{id:int}/cheque")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(ChequeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCheque(int id, [FromBody] CreateChequeRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        // Ensure the cheque is linked to this customer payment
        var chequeRequest = request with { CustomerPaymentId = id };

        var result = await _chequeService.CreateAsync(chequeRequest, userId, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id }, new { chequeId = result.Value!.Id, cheque = result.Value });

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });

        return BadRequest(new { error = result.Error });
    }
}
