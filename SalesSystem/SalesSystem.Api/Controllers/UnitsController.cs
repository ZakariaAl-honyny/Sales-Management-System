using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Units;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for managing measurement units.
/// </summary>
/// <remarks>
/// Requires Manager or Admin role (Policy: ManagerAndAbove).
/// </remarks>
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
    /// Retrieves all units with pagination.
    /// </summary>
    /// <param name="search">Optional search term by unit name.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Items per page (default: 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns paginated list of units.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _unitService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Retrieves a unit by its ID.
    /// </summary>
    /// <param name="id">Unit ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the unit if found.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _unitService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new unit.
    /// </summary>
    /// <param name="request">Create unit request with Name and optional Symbol.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the created unit with ID.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest request, CancellationToken ct)
    {
        var result = await _unitService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing unit.
    /// </summary>
    /// <param name="id">Unit ID to update.</param>
    /// <param name="request">Update unit request with all unit fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the updated unit.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUnitRequest request, CancellationToken ct)
    {
        var result = await _unitService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a unit.
    /// </summary>
    /// <param name="id">Unit ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message with deleted ID.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _unitService.DeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف بنجاح", id });
        return BadRequest(new { error = result.Error });
    }
}