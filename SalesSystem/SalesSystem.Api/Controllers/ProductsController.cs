using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Products;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Products management API
/// </summary>
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
    /// Gets all products with optional filtering and pagination
    /// </summary>
    /// <param name="search">Search by name, code, or barcode</param>
    /// <param name="categoryId">Filter by category ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of products</returns>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search, 
        [FromQuery] int? categoryId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken ct = default)
    {
        var result = await _productService.GetAllAsync(search, categoryId, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Product details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _productService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="request">Product creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(ProductDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Product update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a product (soft delete)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _productService.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok("Product deleted successfully") : BadRequest(new { error = result.Error });
    }
}