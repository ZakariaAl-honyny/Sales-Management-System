using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Services.Api;

public sealed class DashboardApiService : IDashboardApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/dashboard";

    public DashboardApiService(HttpClientService http) => _http = http;

    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<DashboardSummaryDto>($"{BasePath}/summary", ct);
    }
}

