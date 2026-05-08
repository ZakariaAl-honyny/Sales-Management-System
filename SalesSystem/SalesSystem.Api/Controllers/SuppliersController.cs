using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Suppliers;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for managing suppliers.
/// </summary>
/// <remarks>
/// Requires Manager or Admin role (Policy: ManagerAndAbove).
/// </remarks>
[ApiController]
[Route("api/v1/suppliers")]
[Authorize(Policy = "ManagerAndAbove")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>
    /// Retrieves all suppliers with pagination.
    /// </summary>
    /// <param name="search">Optional search term by supplier name or code.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Items per page (default: 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns paginated list of suppliers.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _supplierService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Retrieves a supplier by its ID.
    /// </summary>
    /// <param name="id">Supplier ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the supplier if found.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _supplierService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new supplier.
    /// </summary>
    /// <param name="request">Create supplier request with Name, Code, and optional fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the created supplier with ID.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        var result = await _supplierService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing supplier.
    /// </summary>
    /// <param name="id">Supplier ID to update.</param>
    /// <param name="request">Update supplier request with all supplier fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the updated supplier.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
    {
        var result = await _supplierService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a supplier.
    /// </summary>
    /// <param name="id">Supplier ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message with deleted ID.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _supplierService.DeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف بنجاح", id });
        return BadRequest(new { error = result.Error });
    }
}