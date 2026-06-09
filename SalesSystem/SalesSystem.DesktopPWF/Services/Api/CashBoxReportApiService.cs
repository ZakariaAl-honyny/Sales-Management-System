using System.Net.Http;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public class CashBoxReportApiService : ApiServiceBase, ICashBoxReportApiService
{
    private const string BasePath = "api/v1/reports/cash-boxes";

    public CashBoxReportApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<CashBoxSummaryDto>>> GetCashBoxSummaryAsync(DateTime? asOfDate = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/summary";
        if (asOfDate.HasValue)
            url += $"?asOfDate={asOfDate.Value:yyyy-MM-dd}";
        return await ExecuteAsync<List<CashBoxSummaryDto>>(
            () => _httpClient.GetAsync(url, ct),
            "CashBoxReportApiService.GetCashBoxSummaryAsync");
    }

    public async Task<Result<List<DailyClosureReportDto>>> GetDailyClosureReportAsync(DateTime from, DateTime to, int? cashBoxId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/daily-closures?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (cashBoxId.HasValue)
            url += $"&cashBoxId={cashBoxId.Value}";
        return await ExecuteAsync<List<DailyClosureReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "CashBoxReportApiService.GetDailyClosureReportAsync");
    }

    public async Task<Result<List<CashTransactionDetailDto>>> GetCashTransactionDetailsAsync(int cashBoxId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<CashTransactionDetailDto>>(
            () => _httpClient.GetAsync($"{BasePath}/{cashBoxId}/transactions?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "CashBoxReportApiService.GetCashTransactionDetailsAsync");
    }
}
