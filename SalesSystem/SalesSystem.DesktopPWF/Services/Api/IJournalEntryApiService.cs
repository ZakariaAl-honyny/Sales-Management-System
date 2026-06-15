using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service interface for Journal Entry operations.
/// Provides access to journal entry list, detail, balance, and ledger endpoints.
/// </summary>
public interface IJournalEntryApiService
{
    /// <summary>
    /// Gets a paginated list of journal entries.
    /// </summary>
    Task<Result<List<JournalEntryListDto>>> GetAllAsync(int page = 1, int pageSize = 50);

    /// <summary>
    /// Gets a single journal entry with its lines.
    /// </summary>
    Task<Result<JournalEntryDetailDto>> GetByIdAsync(int id);

    /// <summary>
    /// Gets the balance for a specific account as of an optional date.
    /// </summary>
    Task<Result<AccountBalanceDto>> GetBalanceAsync(int accountId, DateTime? asOfDate = null);

    /// <summary>
    /// Gets the ledger (detailed transactions) for a specific account within a date range.
    /// </summary>
    Task<Result<AccountLedgerDto>> GetLedgerAsync(int accountId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Creates a new journal entry (Draft status).
    /// </summary>
    Task<Result<int>> CreateAsync(CreateJournalEntryRequest request);

    /// <summary>
    /// Posts a Draft journal entry (transitions to Posted status).
    /// </summary>
    Task<Result<JournalEntryDetailDto>> PostAsync(int id);

    /// <summary>
    /// Cancels a Posted journal entry (transitions to Cancelled status).
    /// </summary>
    Task<Result<JournalEntryDetailDto>> CancelAsync(int id);
}
