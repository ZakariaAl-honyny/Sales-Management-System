using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for user-related reports (Phase 31).
/// </summary>
public interface IUserReportService
{
    /// <summary>
    /// Gets user activity log within a date range.
    /// </summary>
    Task<Result<List<UserActivityReportDto>>> GetUserActivityAsync(int? userId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Gets login history within a date range.
    /// </summary>
    Task<Result<List<LoginHistoryDto>>> GetLoginHistoryAsync(int? userId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Gets audit trail summary within a date range.
    /// </summary>
    Task<Result<List<AuditTrailSummaryDto>>> GetAuditTrailSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
