using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Warehouses;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Warehouses management API
/// </summary>
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
    /// Gets all warehouses with optional search and pagination
    /// </summary>
    /// <param name="search">Search by name or code</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of warehouses</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WarehouseDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _warehouseService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a warehouse by ID
    /// </summary>
    /// <param name="id">Warehouse ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Warehouse details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WarehouseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _warehouseService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new warehouse
    /// </summary>
    /// <param name="request">Warehouse creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created warehouse</returns>
    [HttpPost]
    [ProducesResponseType(typeof(WarehouseDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseRequest request, CancellationToken ct)
    {
        var result = await _warehouseService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing warehouse
    /// </summary>
    /// <param name="id">Warehouse ID</param>
    /// <param name="request">Warehouse update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated warehouse</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(WarehouseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseRequest request, CancellationToken ct)
    {
        var result = await _warehouseService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a warehouse (soft delete)
    /// </summary>
    /// <param name="id">Warehouse ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _warehouseService.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok("Warehouse deleted successfully") : BadRequest(new { error = result.Error });
    }
}