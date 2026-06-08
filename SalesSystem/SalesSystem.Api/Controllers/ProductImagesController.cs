using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId:int}/images")]
[Authorize]
public class ProductImagesController : ControllerBase
{
    private readonly IProductImageService _service;

    public ProductImagesController(IProductImageService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lists all active images for a product.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetByProduct(int productId, CancellationToken ct)
    {
        if (productId <= 0)
            return BadRequest(new { error = "معرف المنتج مطلوب" });

        var result = await _service.GetByProductAsync(productId, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Uploads/creates a new product image record.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Create(int productId, [FromBody] CreateProductImageRequest request, CancellationToken ct)
    {
        if (productId <= 0)
            return BadRequest(new { error = "معرف المنتج مطلوب" });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        // Ensure the request ProductId matches the route
        if (request.ProductId != productId)
            return BadRequest(new { error = "معرف المنتج في المسار لا يتطابق مع الطلب" });

        var result = await _service.CreateAsync(request, userId, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetByProduct), new { productId }, result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Sets an image as the primary image for the product.
    /// </summary>
    [HttpPut("{id:int}/primary")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> SetPrimary(int productId, int id, CancellationToken ct)
    {
        var result = await _service.SetPrimaryAsync(productId, id, ct);
        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Soft-deletes (deactivates) a product image.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Deactivate(int productId, int id, CancellationToken ct)
    {
        var result = await _service.DeactivateAsync(id, ct);
        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
