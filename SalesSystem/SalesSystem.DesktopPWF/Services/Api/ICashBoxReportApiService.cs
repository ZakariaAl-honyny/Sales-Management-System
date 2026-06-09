using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface ICashBoxReportApiService
{
    Task<Result<List<CashBoxSummaryDto>>> GetCashBoxSummaryAsync(DateTime? asOfDate = null, CancellationToken ct = default);
    Task<Result<List<DailyClosureReportDto>>> GetDailyClosureReportAsync(DateTime from, DateTime to, int? cashBoxId = null, CancellationToken ct = default);
    Task<Result<List<CashTransactionDetailDto>>> GetCashTransactionDetailsAsync(int cashBoxId, DateTime from, DateTime to, CancellationToken ct = default);
}
