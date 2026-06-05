using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IJournalEntryService
{
    /// <summary>
    /// Creates and posts a balanced journal entry.
    /// </summary>
    Task<Result<int>> CreateJournalEntryAsync(CreateJournalEntryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets the account balance as of an optional date (default = all time).
    /// </summary>
    Task<Result<AccountBalanceDto>> GetAccountBalanceAsync(int accountId, DateTime? asOfDate = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a detailed account ledger (statement) for a date range.
    /// </summary>
    Task<Result<AccountLedgerDto>> GetAccountLedgerAsync(int accountId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
}
