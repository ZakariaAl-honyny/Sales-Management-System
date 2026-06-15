using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing fiscal years (open/close lifecycle).
/// </summary>
public interface IFiscalYearService
{
    /// <summary>
    /// Gets all fiscal years, ordered by Year descending.
    /// </summary>
    Task<Result<List<FiscalYearDto>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a fiscal year by its ID.
    /// </summary>
    Task<Result<FiscalYearDto>> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets a fiscal year by its calendar year.
    /// </summary>
    Task<Result<FiscalYearDto>> GetByYearAsync(int year, CancellationToken ct = default);

    /// <summary>
    /// Creates a new fiscal year.
    /// </summary>
    Task<Result<FiscalYearDto>> CreateAsync(CreateFiscalYearRequest request, int userId, CancellationToken ct = default);

    /// <summary>
    /// Opens (reopens) a fiscal year that was previously closed.
    /// </summary>
    Task<Result<FiscalYearDto>> OpenAsync(int id, int userId, CancellationToken ct = default);

    /// <summary>
    /// Closes an open fiscal year.
    /// </summary>
    Task<Result<FiscalYearDto>> CloseAsync(int id, int userId, CancellationToken ct = default);
}
