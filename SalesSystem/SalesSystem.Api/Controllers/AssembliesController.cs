using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for Bills of Materials and assembly production operations.
/// </summary>
/// <remarks>
/// - GET endpoints: All Staff roles (Admin, Manager, Cashier)<br/>
/// - POST/PUT endpoints: Manager and Admin only (Policy: ManagerAndAbove)<br/>
/// - DELETE endpoint: Manager and Admin only (Policy: ManagerAndAbove)
/// </remarks>
[ApiController]
[Route("api/v1/assemblies")]
[Authorize]
public class AssembliesController : ControllerBase
{
    private readonly IAssemblyService _assemblyService;

    public AssembliesController(IAssemblyService assemblyService)
    {
        _assemblyService = assemblyService;
    }

    /// <summary>
    /// Gets the current user ID from JWT claims.
    /// </summary>
    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
    }

    // ═══════════════════════════════════════════════════════════════
    // BOM CRUD Endpoints
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets all Bill of Materials entries across all assembly products.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _assemblyService.GetAllBomsAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a single Bill of Materials entry by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _assemblyService.GetBomByIdAsync(id, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == ErrorCodes.NotFound)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets all Bill of Materials entries for a specific assembly product.
    /// </summary>
    [HttpGet("by-product/{productId:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProduct(int productId, CancellationToken ct)
    {
        var result = await _assemblyService.GetBomsForAssemblyAsync(productId, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == ErrorCodes.NotFound)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new Bill of Materials entry linking a component to an assembly product.
    /// Validates that both products exist, the component unit belongs to the component product,
    /// and no duplicate BOM entry exists.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateBillOfMaterialRequest request, CancellationToken ct)
    {
        var result = await _assemblyService.CreateBomAsync(request, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        if (result.ErrorCode == ErrorCodes.DuplicateEntry)
            return Conflict(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing Bill of Materials entry.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBillOfMaterialRequest request, CancellationToken ct)
    {
        var result = await _assemblyService.UpdateBomAsync(id, request, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Soft-deletes a Bill of Materials entry.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _assemblyService.DeleteBomAsync(id, ct);
        if (result.IsSuccess)
            return NoContent();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    // ═══════════════════════════════════════════════════════════════
    // Assembly Production Endpoints
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Produces an assembly by deducting all component quantities from inventory
    /// using FIFO/FEFO allocation and adding the finished assembly product as a new inventory batch.
    /// </summary>
    /// <remarks>
    /// The operation is atomic — if any component has insufficient stock, the entire production is rolled back.
    /// Component consumption is recorded via FIFO allocation, and the finished product gets a new inventory batch.
    /// </remarks>
    [HttpPost("produce")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Produce([FromBody] ProduceAssemblyRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _assemblyService.ProduceAsync(request, userId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        if (result.ErrorCode == ErrorCodes.InsufficientStock)
            return BadRequest(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
