using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing fiscal year closing operations.
/// </summary>
public interface IAnnualClosingService
{
    /// <summary>
    /// Closes the specified fiscal year: zeros out Revenue/Expense accounts,
    /// transfers net income/loss to Retained Earnings (AccountCode 3102).
    /// Returns the journal entry ID of the closing entry.
    /// </summary>
    Task<Result<int>> CloseFiscalYearAsync(int fiscalYear, int closedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the given fiscal year is already closed.
    /// </summary>
    Task<Result<bool>> IsFiscalYearClosedAsync(int fiscalYear, CancellationToken ct = default);
}
