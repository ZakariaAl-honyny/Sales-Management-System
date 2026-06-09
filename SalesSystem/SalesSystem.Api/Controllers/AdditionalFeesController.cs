using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Additional fees management API for purchase invoices
/// </summary>
[ApiController]
[Route("api/v1/purchase-invoices")]
[Authorize(Policy = "ManagerAndAbove")]
public class AdditionalFeesController : ControllerBase
{
    private readonly IAdditionalFeeService _additionalFeeService;

    public AdditionalFeesController(IAdditionalFeeService additionalFeeService)
    {
        _additionalFeeService = additionalFeeService;
    }

    /// <summary>
    /// Gets all additional fees for a purchase invoice
    /// </summary>
    [HttpGet("{invoiceId:int}/additional-fees")]
    [ProducesResponseType(typeof(List<AdditionalFeeDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetFeesByInvoice(int invoiceId, CancellationToken ct)
    {
        var result = await _additionalFeeService.GetFeesByInvoiceAsync(invoiceId, ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new additional fee for a purchase invoice
    /// </summary>
    [HttpPost("{invoiceId:int}/additional-fees")]
    [ProducesResponseType(typeof(AdditionalFeeDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateFee(int invoiceId, [FromBody] CreateAdditionalFeeRequest request, CancellationToken ct)
    {
        var result = await _additionalFeeService.CreateFeeAsync(request, invoiceId, ct);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetFeesByInvoice), new { invoiceId }, result.Value);

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Removes an additional fee
    /// </summary>
    [HttpDelete("additional-fees/{feeId:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveFee(int feeId, CancellationToken ct)
    {
        var result = await _additionalFeeService.RemoveFeeAsync(feeId, ct);

        if (result.IsSuccess)
            return Ok();

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Distributes all additional fees for a purchase invoice across its items
    /// </summary>
    [HttpPost("{invoiceId:int}/distribute-fees")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DistributeFees(int invoiceId, CancellationToken ct)
    {
        var result = await _additionalFeeService.DistributeFeesAsync(invoiceId, ct);

        if (result.IsSuccess)
            return Ok();

        return result.ErrorCode == ErrorCodes.NotFound
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }
}
