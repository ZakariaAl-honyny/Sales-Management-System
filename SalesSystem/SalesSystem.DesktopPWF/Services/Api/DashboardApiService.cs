using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public class DashboardApiService : ApiServiceBase, IDashboardApiService
{
    public DashboardApiService(HttpClient httpClient, ISessionService session) 
        : base(httpClient, session)
    {
    }

    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<DashboardSummaryDto>(
            () => _httpClient.GetAsync("api/v1/dashboard/summary", ct),
            "DashboardApiService.GetSummaryAsync");
    }
}
