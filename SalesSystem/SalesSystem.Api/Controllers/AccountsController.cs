using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for chart of accounts operations: CRUD, tree, balances, ledgers, and system mappings.
/// </summary>
/// <remarks>
/// CRUD endpoints require ManagerAndAbove for write operations and AdminOnly for permanent deletes.
/// Balance/ledger/mappings require AllStaff.
/// </remarks>
[ApiController]
[Route("api/v1/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IJournalEntryService _journalEntryService;
    private readonly ISystemAccountService _systemAccountService;
    private readonly IAccountService _accountService;

    public AccountsController(
        IJournalEntryService journalEntryService,
        ISystemAccountService systemAccountService,
        IAccountService accountService)
    {
        _journalEntryService = journalEntryService;
        _systemAccountService = systemAccountService;
        _accountService = accountService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
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
    public async Task<IActionResult> GetMappings([FromQuery] short? branchId, CancellationToken ct)
    {
        var result = await _systemAccountService.GetAllMappingsAsync(branchId, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == ErrorCodes.NotFound)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    // ═══════════════════════════════════════════════════════
    // Phase 22 — Chart of Accounts CRUD Endpoints
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Gets the full chart of accounts as a hierarchical tree.
    /// </summary>
    [HttpGet("tree")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTree(CancellationToken ct)
    {
        var result = await _accountService.GetTreeAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets all accounts as a flat list ordered by AccountCode.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _accountService.GetAllAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a single account by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _accountService.GetByIdAsync(id, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == ErrorCodes.NotFound)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets accounts filtered by type (1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense).
    /// </summary>
    [HttpGet("by-type/{type:int:min(1):max(5)}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByType(int type, CancellationToken ct)
    {
        if (type < 1 || type > 5)
            return BadRequest(new { error = "نوع الحساب غير صالح — القيم المسموحة: 1-5" });

        var result = await _accountService.GetByTypeAsync((AccountType)type, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new account in the chart of accounts.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(CreateAccountRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _accountService.CreateAsync(request, userId, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateAccountRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _accountService.UpdateAsync(id, request, userId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Soft-deletes an account (sets IsActive = false). Blocks system accounts and parent accounts with children.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _accountService.DeleteAsync(id, ct);
        if (result.IsSuccess)
            return NoContent();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Permanently deletes an account from the database. Admin only.
    /// Blocks system accounts, parent accounts with children, and accounts referenced by transactions.
    /// </summary>
    [HttpDelete("permanent/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PermanentDelete(int id, CancellationToken ct)
    {
        var result = await _accountService.PermanentDeleteAsync(id, ct);
        if (result.IsSuccess)
            return NoContent();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
