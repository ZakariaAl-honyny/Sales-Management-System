using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface IUserReportApiService
{
    Task<Result<List<UserActivityReportDto>>> GetUserActivityAsync(int? userId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<LoginHistoryDto>>> GetLoginHistoryAsync(int? userId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<List<AuditTrailSummaryDto>>> GetAuditTrailSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
