using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Suppliers;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Suppliers management API
/// </summary>
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
    /// Gets all suppliers with optional search and pagination
    /// </summary>
    /// <param name="search">Search by name, code, phone, or email</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of suppliers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SupplierDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _supplierService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a supplier by ID
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Supplier details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SupplierDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _supplierService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new supplier
    /// </summary>
    /// <param name="request">Supplier creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created supplier</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SupplierDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        var result = await _supplierService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing supplier
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="request">Supplier update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated supplier</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SupplierDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
    {
        var result = await _supplierService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a supplier (soft delete)
    /// </summary>
    /// <param name="id">Supplier ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _supplierService.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok("Supplier deleted successfully") : BadRequest(new { error = result.Error });
    }
}