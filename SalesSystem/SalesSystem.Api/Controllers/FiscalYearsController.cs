using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for fiscal year management: create, open, close, and query.
/// </summary>
[ApiController]
[Route("api/v1/fiscal-years")]
[Authorize]
public class FiscalYearsController : ControllerBase
{
    private readonly IFiscalYearService _fiscalYearService;

    public FiscalYearsController(IFiscalYearService fiscalYearService)
    {
        _fiscalYearService = fiscalYearService;
    }

    /// <summary>
    /// Gets all fiscal years (newest first).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _fiscalYearService.GetAllAsync(ct);
        return result.IsSuccess
            ? Ok(result.Value ?? new List<FiscalYearDto>())
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a fiscal year by its ID.
    /// </summary>
    [HttpGet("{id:int:min(1)}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _fiscalYearService.GetByIdAsync(id, ct);
        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a fiscal year by its calendar year.
    /// </summary>
    [HttpGet("by-year/{year:int:min(2000)}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByYear(int year, CancellationToken ct)
    {
        var result = await _fiscalYearService.GetByYearAsync(year, ct);
        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new fiscal year (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFiscalYearRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "المستخدم غير مصرح له" });

        var result = await _fiscalYearService.CreateAsync(request, userId, ct);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Opens (reopens) a fiscal year (Admin only).
    /// </summary>
    [HttpPut("{id:int:min(1)}/open")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Open(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "المستخدم غير مصرح له" });

        var result = await _fiscalYearService.OpenAsync(id, userId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Closes a fiscal year (Admin only).
    /// Note: This closes the fiscal year record only. For full annual closing
    /// with zeroing Revenue/Expense accounts, use /api/v1/journal-entries/close-fiscal-year.
    /// </summary>
    [HttpPut("{id:int:min(1)}/close")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "المستخدم غير مصرح له" });

        var result = await _fiscalYearService.CloseAsync(id, userId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
