using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for managing products.
/// </summary>
/// <remarks>
/// - GET endpoints: All Staff roles (Admin, Manager, Cashier)<br/>
/// - POST/PUT/DELETE endpoints: Manager and Admin only (Policy: ManagerAndAbove)
/// </remarks>
[ApiController]
[Route("api/v1/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Retrieves all products with pagination and optional filters.
    /// </summary>
    /// <param name="search">Optional search term by product name or code.</param>
    /// <param name="categoryId">Optional category ID filter.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Items per page (default: 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns paginated list of products.</returns>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _productService.GetAllAsync(search, categoryId, page, pageSize, includeInactive, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Retrieves a product by its ID.
    /// </summary>
    /// <param name="id">Product ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the product if found.</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _productService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="request">Create product request with Name, Barcode, CategoryId, MinStock, Description.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the created product with ID.</returns>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">Product ID to update.</param>
    /// <param name="request">Update product request with Name, Barcode, CategoryId, MinStock, Description, IsActive.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the updated product.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a product (Soft Delete).
    /// </summary>
    /// <param name="id">Product ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message with deleted ID.</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _productService.DeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف بنجاح", id });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Permanently deletes a product.
    /// </summary>
    /// <param name="id">Product ID to delete permanently.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message with deleted ID.</returns>
    [HttpDelete("permanent/{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PermanentDelete(int id, CancellationToken ct)
    {
        var result = await _productService.PermanentDeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف النهائي بنجاح", id });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Retrieves products that are expiring within the specified threshold days.
    /// </summary>
    /// <param name="thresholdDays">Number of days from today (default: 30). Products expiring within this window are returned.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of products expiring within the threshold.</returns>
    [HttpGet("expiring")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetExpiring([FromQuery] int thresholdDays = 30, CancellationToken ct = default)
    {
        var result = await _productService.GetExpiringProductsAsync(thresholdDays, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("barcode/{barcode}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByBarcode(string barcode, CancellationToken ct)
    {
        var result = await _productService.GetByBarcodeAsync(barcode, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Uploads a product image. Accepts multipart/form-data with an "image" file field.
    /// </summary>
    /// <param name="id">Product ID.</param>
    /// <param name="image">The image file (IFormFile).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the updated product with new ImagePath.</returns>
    [HttpPost("{id:int}/image")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage(int id, IFormFile image, CancellationToken ct)
    {
        if (image == null || image.Length == 0)
            return BadRequest(new { error = "ملف الصورة مطلوب" });

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        var imageBytes = ms.ToArray();

        var result = await _productService.UploadImageAsync(id, imageBytes, image.FileName, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}