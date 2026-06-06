using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing fiscal year closing operations.
/// </summary>
public interface IAnnualClosingService
{
    /// <summary>
    /// Closes the specified fiscal year: zeros out Revenue/Expense accounts,
    /// transfers net income/loss to Retained Earnings (AccountCode 3102).
    /// </summary>
    Task<Result<FiscalYearClosureDto>> CloseFiscalYearAsync(int fiscalYear, int closedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the given fiscal year is already closed.
    /// </summary>
    Task<Result<bool>> IsFiscalYearClosedAsync(int fiscalYear, CancellationToken ct = default);

    /// <summary>
    /// Gets all fiscal year closures.
    /// </summary>
    Task<Result<List<FiscalYearClosureDto>>> GetAllClosuresAsync(CancellationToken ct = default);
}
