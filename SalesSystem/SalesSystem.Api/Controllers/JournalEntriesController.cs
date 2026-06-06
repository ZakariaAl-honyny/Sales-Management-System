using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for journal entry operations: creation, fiscal year closing, and closure queries.
/// </summary>
/// <remarks>
/// - POST endpoints require Manager and above (Policy: ManagerAndAbove)<br/>
/// - GET endpoints: Manager and above (Policy: ManagerAndAbove)
/// </remarks>
[ApiController]
[Route("api/v1/journal-entries")]
[Authorize]
public class JournalEntriesController : ControllerBase
{
    private readonly IJournalEntryService _journalEntryService;
    private readonly IAnnualClosingService _annualClosingService;

    public JournalEntriesController(
        IJournalEntryService journalEntryService,
        IAnnualClosingService annualClosingService)
    {
        _journalEntryService = journalEntryService;
        _annualClosingService = annualClosingService;
    }

    /// <summary>
    /// Creates and posts a balanced journal entry.
    /// </summary>
    /// <param name="request">Journal entry with lines (debits must equal credits).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the created journal entry ID with 201 status.</returns>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateJournalEntryRequest request, CancellationToken ct)
    {
        var result = await _journalEntryService.CreateJournalEntryAsync(request, ct);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetBalanceStub), new { id = result.Value }, new
            {
                id = result.Value,
                message = "تم إنشاء القيد المحاسبي وترحيله بنجاح"
            });
        }

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Closes the specified fiscal year: zeros out Revenue/Expense accounts
    /// and transfers net income/loss to Retained Earnings.
    /// </summary>
    /// <param name="request">Fiscal year to close.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Fiscal year closure details.</returns>
    [HttpPost("close-fiscal-year")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CloseFiscalYear([FromBody] CloseFiscalYearRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "المستخدم غير مصرح له" });

        var result = await _annualClosingService.CloseFiscalYearAsync(request.FiscalYear, userId, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets all fiscal year closures.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of fiscal year closures.</returns>
    [HttpGet("closed-years")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllClosures(CancellationToken ct)
    {
        var result = await _annualClosingService.GetAllClosuresAsync(ct);
        if (result.IsSuccess)
        {
            return Ok(result.Value ?? new List<FiscalYearClosureDto>());
        }

        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Checks if a specific fiscal year is closed.
    /// </summary>
    /// <param name="fiscalYear">Fiscal year to check (e.g., 2026).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the fiscal year is closed, false otherwise.</returns>
    [HttpGet("closed-years/{fiscalYear:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IsFiscalYearClosed(int fiscalYear, CancellationToken ct)
    {
        var result = await _annualClosingService.IsFiscalYearClosedAsync(fiscalYear, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == ErrorCodes.NotFound)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(new { fiscalYear, isClosed = result.Value });
    }

    /// <summary>
    /// Stub endpoint for CreatedAtAction reference. Not callable directly.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public IActionResult GetBalanceStub(int id)
    {
        return Ok(new { id });
    }
}
