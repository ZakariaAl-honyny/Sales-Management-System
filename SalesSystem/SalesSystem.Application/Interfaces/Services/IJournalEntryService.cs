using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IJournalEntryService
{
    /// <summary>
    /// Creates a journal entry in Draft status.
    /// </summary>
    /// <param name="request">Journal entry data (no CreatedBy — extracted from JWT).</param>
    /// <param name="userId">Authenticated user ID from JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<int>> CreateJournalEntryAsync(CreateJournalEntryRequest request, int userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a paginated list of journal entries, ordered by TransactionDate descending.
    /// </summary>
    Task<Result<List<JournalEntryListDto>>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);

    /// <summary>
    /// Gets a single journal entry with all its lines by ID.
    /// </summary>
    Task<Result<JournalEntryDetailDto>> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets the account balance as of an optional date (default = all time).
    /// </summary>
    Task<Result<AccountBalanceDto>> GetAccountBalanceAsync(int accountId, DateTime? asOfDate = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a detailed account ledger (statement) for a date range.
    /// </summary>
    Task<Result<AccountLedgerDto>> GetAccountLedgerAsync(int accountId, DateTime startDate, DateTime endDate, CancellationToken ct = default);

    /// <summary>
    /// Posts a Draft journal entry (transitions to Posted status).
    /// </summary>
    /// <param name="id">Journal entry ID.</param>
    /// <param name="userId">Authenticated user ID from JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The posted journal entry detail.</returns>
    Task<Result<JournalEntryDetailDto>> PostJournalEntryAsync(int id, int userId, CancellationToken ct = default);

    /// <summary>
    /// Cancels a Posted journal entry (transitions to Cancelled status).
    /// </summary>
    /// <param name="id">Journal entry ID.</param>
    /// <param name="userId">Authenticated user ID from JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cancelled journal entry detail.</returns>
    Task<Result<JournalEntryDetailDto>> CancelJournalEntryAsync(int id, int userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a Draft journal entry (permanently removed from DB).
    /// </summary>
    /// <param name="id">Journal entry ID.</param>
    /// <param name="userId">Authenticated user ID from JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> DeleteDraftAsync(int id, int userId, CancellationToken ct = default);
}
