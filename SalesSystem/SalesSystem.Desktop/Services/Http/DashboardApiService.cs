using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class DashboardApiService : IDashboardApiService
{
    private readonly HttpClient _httpClient;
    public DashboardApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<DashboardSummaryDto>("api/v1/dashboard/summary", ct);
            return Result<DashboardSummaryDto>.Success(response!);
        }
        catch (Exception ex) { return Result<DashboardSummaryDto>.Failure(ex.Message); }
    }
}
