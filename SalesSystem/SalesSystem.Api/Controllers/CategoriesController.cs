using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Categories;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Categories management API
/// </summary>
[ApiController]
[Route("api/v1/categories")]
[Authorize(Policy = "ManagerAndAbove")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Gets all categories with optional search and pagination
    /// </summary>
    /// <param name="search">Search by category name</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of categories</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CategoryDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _categoryService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a category by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Category details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _categoryService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new category
    /// </summary>
    /// <param name="request">Category creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created category</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="request">Category update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated category</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a category (soft delete)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _categoryService.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok("Category deleted successfully") : BadRequest(new { error = result.Error });
    }
}