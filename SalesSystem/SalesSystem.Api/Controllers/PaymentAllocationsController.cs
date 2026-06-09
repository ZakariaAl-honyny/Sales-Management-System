using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Payment allocations API — manage how payments are distributed across multiple invoices.
/// One payment can settle multiple invoices; one invoice can be settled by multiple payments.
/// </summary>
[ApiController]
[Route("api/v1/payment-allocations")]
[Authorize]
public class PaymentAllocationsController : ControllerBase
{
    private readonly IPaymentAllocationService _allocationService;

    public PaymentAllocationsController(IPaymentAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    /// <summary>
    /// Gets all allocations for a specific payment.
    /// </summary>
    /// <param name="paymentId">The payment ID.</param>
    /// <param name="paymentType">1 = CustomerPayment, 2 = SupplierPayment.</param>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(List<PaymentAllocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByPayment(
        [FromQuery] int paymentId,
        [FromQuery] byte paymentType,
        CancellationToken ct = default)
    {
        if (paymentId <= 0)
            return BadRequest(new { error = "معرف الدفع غير صالح" });

        if (paymentType != 1 && paymentType != 2)
            return BadRequest(new { error = "نوع الدفع غير صالح — 1 لسداد العميل، 2 لسداد المورد" });

        var result = await _allocationService.GetAllocationsForPaymentAsync(paymentId, paymentType, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Replaces all allocations for a payment with a new set.
    /// </summary>
    [HttpPut]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAllocations(
        [FromQuery] int paymentId,
        [FromQuery] byte paymentType,
        [FromBody] UpdateAllocationsRequest request,
        CancellationToken ct = default)
    {
        if (paymentId <= 0)
            return BadRequest(new { error = "معرف الدفع غير صالح" });

        if (paymentType != 1 && paymentType != 2)
            return BadRequest(new { error = "نوع الدفع غير صالح — 1 لسداد العميل، 2 لسداد المورد" });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var _)) return Unauthorized();

        var result = await _allocationService.UpdateAllocationsAsync(paymentId, paymentType, request, ct);
        if (result.IsSuccess) return Ok(new { message = "تم تحديث التوزيعات بنجاح" });
        if (result.ErrorCode == ErrorCodes.NotFound) return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
