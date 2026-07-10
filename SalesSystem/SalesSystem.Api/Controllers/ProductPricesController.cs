using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/product-prices")]
[Authorize]
public class ProductPricesController : ControllerBase
{
    private readonly IProductPriceService _service;

    public ProductPricesController(IProductPriceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets the effective price for a product unit on a given date.
    /// </summary>
    [HttpGet("effective")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetEffectivePrice(
        [FromQuery] int productUnitId,
        [FromQuery] DateTime? effectiveDate = null,
        CancellationToken ct = default)
    {
        if (productUnitId <= 0)
            return BadRequest(new { error = "معرف وحدة المنتج مطلوب" });

        var result = await _service.GetEffectivePriceAsync(productUnitId, effectiveDate, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets the effective price for an invoice line (convenience — uses UtcNow).
    /// </summary>
    [HttpGet("effective/invoice")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetEffectivePriceForInvoice(
        [FromQuery] int productUnitId,
        CancellationToken ct = default)
    {
        if (productUnitId <= 0)
            return BadRequest(new { error = "معرف وحدة المنتج مطلوب" });

        var result = await _service.GetEffectivePriceForInvoiceAsync(productUnitId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets all active prices for a specific product unit.
    /// </summary>
    [HttpGet("by-unit/{productUnitId:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetByProductUnit(int productUnitId, CancellationToken ct)
    {
        if (productUnitId <= 0)
            return BadRequest(new { error = "معرف وحدة المنتج مطلوب" });

        var result = await _service.GetByProductUnitAsync(productUnitId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a single price by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new product price entry.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Create([FromBody] CreateProductPriceRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.CreateAsync(request, userId, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing product price entry.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductPriceRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _service.UpdateAsync(id, request, userId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Soft-deletes (deactivates) a product price entry.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var result = await _service.DeactivateAsync(id, ct);
        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
