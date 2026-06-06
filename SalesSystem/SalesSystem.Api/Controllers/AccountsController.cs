using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for chart of accounts operations: balances, ledgers, and system mappings.
/// </summary>
/// <remarks>
/// All endpoints require at minimum Manager role (Policy: ManagerAndAbove).
/// </remarks>
[ApiController]
[Route("api/v1/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IJournalEntryService _journalEntryService;
    private readonly ISystemAccountService _systemAccountService;

    public AccountsController(
        IJournalEntryService journalEntryService,
        ISystemAccountService systemAccountService)
    {
        _journalEntryService = journalEntryService;
        _systemAccountService = systemAccountService;
    }

    /// <summary>
    /// Gets the account balance as of an optional date.
    /// </summary>
    /// <param name="id">Account ID.</param>
    /// <param name="asOfDate">Optional date filter (default = all time).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Account balance with debit/credit totals.</returns>
    [HttpGet("{id:int}/balance")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(int id, [FromQuery] DateTime? asOfDate, CancellationToken ct)
    {
        var result = await _journalEntryService.GetAccountBalanceAsync(id, asOfDate, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == ErrorCodes.NotFound)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a detailed account ledger (statement) for a date range.
    /// </summary>
    /// <param name="id">Account ID.</param>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Account ledger with opening balance, lines, and closing balance.</returns>
    [HttpGet("{id:int}/ledger")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLedger(
        int id,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct)
    {
        var result = await _journalEntryService.GetAccountLedgerAsync(id, startDate, endDate, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == ErrorCodes.NotFound)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets system account mappings (which accounts are used for sales, purchases, cash, etc.).
    /// </summary>
    /// <param name="branchId">Optional branch ID filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>System account mappings.</returns>
    [HttpGet("mappings")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMappings([FromQuery] int? branchId, CancellationToken ct)
    {
        var result = await _systemAccountService.GetMappingsAsync(branchId, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == ErrorCodes.NotFound)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }
}
