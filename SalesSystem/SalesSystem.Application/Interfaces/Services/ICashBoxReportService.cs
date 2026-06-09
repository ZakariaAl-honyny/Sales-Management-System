using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for cash box-related reports (Phase 31).
/// </summary>
public interface ICashBoxReportService
{
    /// <summary>
    /// Gets cash box summary (opening, income, expense, closing) for each cash box.
    /// </summary>
    Task<Result<List<CashBoxSummaryDto>>> GetCashBoxSummaryAsync(DateTime? asOfDate = null, CancellationToken ct = default);

    /// <summary>
    /// Gets daily closure report for cash boxes.
    /// </summary>
    Task<Result<List<DailyClosureReportDto>>> GetDailyClosureReportAsync(DateTime from, DateTime to, int? cashBoxId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets transaction details for a specific cash box.
    /// </summary>
    Task<Result<List<CashTransactionDetailDto>>> GetCashTransactionDetailsAsync(int cashBoxId, DateTime from, DateTime to, CancellationToken ct = default);
}
