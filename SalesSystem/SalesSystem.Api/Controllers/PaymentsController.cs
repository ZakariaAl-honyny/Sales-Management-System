using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Payments;
using SalesSystem.Contracts.DTOs;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Payments management API (customer and supplier payments)
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Creates a customer payment (reduces customer balance)
    /// </summary>
    /// <param name="request">Customer payment creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created payment</returns>
    [HttpPost("customer")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(CustomerPaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomerPayment([FromBody] CreateCustomerPaymentRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _paymentService.CreateCustomerPaymentAsync(request, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets all customer payments with optional filtering and pagination
    /// </summary>
    /// <param name="customerId">Filter by customer ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of customer payments</returns>
    [HttpGet("customer")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(PagedResult<CustomerPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCustomerPayments(
        [FromQuery] int? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _paymentService.GetCustomerPaymentsAsync(customerId, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Creates a supplier payment (reduces supplier balance)
    /// </summary>
    /// <param name="request">Supplier payment creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created payment</returns>
    [HttpPost("supplier")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(SupplierPaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSupplierPayment([FromBody] CreateSupplierPaymentRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _paymentService.CreateSupplierPaymentAsync(request, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets all supplier payments with optional filtering and pagination
    /// </summary>
    /// <param name="supplierId">Filter by supplier ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of supplier payments</returns>
    [HttpGet("supplier")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(PagedResult<SupplierPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSupplierPayments(
        [FromQuery] int? supplierId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _paymentService.GetSupplierPaymentsAsync(supplierId, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}