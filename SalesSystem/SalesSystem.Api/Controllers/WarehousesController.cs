using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Warehouses;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for managing warehouses.
/// </summary>
/// <remarks>
/// Requires Admin role only (Policy: AdminOnly).
/// </remarks>
[ApiController]
[Route("api/v1/warehouses")]
[Authorize(Policy = "AdminOnly")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    /// <summary>
    /// Retrieves all warehouses with pagination.
    /// </summary>
    /// <param name="search">Optional search term by warehouse name or code.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Items per page (default: 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns paginated list of warehouses.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _warehouseService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Retrieves a warehouse by its ID.
    /// </summary>
    /// <param name="id">Warehouse ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the warehouse if found.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _warehouseService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new warehouse.
    /// </summary>
    /// <param name="request">Create warehouse request with Name, Code, and optional Location.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the created warehouse with ID.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseRequest request, CancellationToken ct)
    {
        var result = await _warehouseService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing warehouse.
    /// </summary>
    /// <param name="id">Warehouse ID to update.</param>
    /// <param name="request">Update warehouse request with all warehouse fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the updated warehouse.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseRequest request, CancellationToken ct)
    {
        var result = await _warehouseService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a warehouse.
    /// </summary>
    /// <param name="id">Warehouse ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message with deleted ID.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _warehouseService.DeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف بنجاح", id });
        return BadRequest(new { error = result.Error });
    }
}