using System.Net.Http;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public class UserReportApiService : ApiServiceBase, IUserReportApiService
{
    private const string BasePath = "api/v1/reports/users";

    public UserReportApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<UserActivityReportDto>>> GetUserActivityAsync(int? userId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var url = $"{BasePath}/activity?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (userId.HasValue)
            url += $"&userId={userId.Value}";
        return await ExecuteAsync<List<UserActivityReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "UserReportApiService.GetUserActivityAsync");
    }

    public async Task<Result<List<LoginHistoryDto>>> GetLoginHistoryAsync(int? userId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var url = $"{BasePath}/login-history?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (userId.HasValue)
            url += $"&userId={userId.Value}";
        return await ExecuteAsync<List<LoginHistoryDto>>(
            () => _httpClient.GetAsync(url, ct),
            "UserReportApiService.GetLoginHistoryAsync");
    }

    public async Task<Result<List<AuditTrailSummaryDto>>> GetAuditTrailSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<AuditTrailSummaryDto>>(
            () => _httpClient.GetAsync($"{BasePath}/audit-summary?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "UserReportApiService.GetAuditTrailSummaryAsync");
    }
}
