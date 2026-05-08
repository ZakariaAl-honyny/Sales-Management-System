using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Units;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Units of measurement management API
/// </summary>
[ApiController]
[Route("api/v1/units")]
[Authorize(Policy = "ManagerAndAbove")]
public class UnitsController : ControllerBase
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    /// <summary>
    /// Gets all units with optional search and pagination
    /// </summary>
    /// <param name="search">Search by unit name</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of units</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UnitDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _unitService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a unit by ID
    /// </summary>
    /// <param name="id">Unit ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Unit details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UnitDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _unitService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new unit
    /// </summary>
    /// <param name="request">Unit creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created unit</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UnitDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest request, CancellationToken ct)
    {
        var result = await _unitService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing unit
    /// </summary>
    /// <param name="id">Unit ID</param>
    /// <param name="request">Unit update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated unit</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UnitDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUnitRequest request, CancellationToken ct)
    {
        var result = await _unitService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a unit (soft delete)
    /// </summary>
    /// <param name="id">Unit ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _unitService.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok("Unit deleted successfully") : BadRequest(new { error = result.Error });
    }
}