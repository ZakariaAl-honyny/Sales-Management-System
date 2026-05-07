using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Categories;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for managing product categories.
/// </summary>
/// <remarks>
/// Requires Manager or Admin role (Policy: ManagerAndAbove).
/// </remarks>
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
    /// Retrieves all categories with pagination.
    /// </summary>
    /// <param name="search">Optional search term for category name.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Items per page (default: 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns paginated list of categories.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _categoryService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Retrieves a category by its ID.
    /// </summary>
    /// <param name="id">Category ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the category if found.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _categoryService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="request">Create category request with Name and optional Description.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the created category with ID.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">Category ID to update.</param>
    /// <param name="request">Update category request with Id, Name, Description, and IsActive.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the updated category.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a category.
    /// </summary>
    /// <param name="id">Category ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message with deleted ID.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _categoryService.DeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف بنجاح", id });
        return BadRequest(new { error = result.Error });
    }
}