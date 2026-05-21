using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Sales Returns
/// </summary>
public class SalesReturnApiService : ApiServiceBase, ISalesReturnApiService
{
    private const string BasePath = "api/v1/sales-returns";

    public SalesReturnApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SalesReturnDto>>> GetAllAsync(
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
            $"includeInactive={includeInactive}"
        };

        if (!string.IsNullOrEmpty(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (from.HasValue)
            queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)
            queryParams.Add($"to={to.Value:yyyy-MM-dd}");

        var query = string.Join("&", queryParams);
        return await ExecutePagedAsync<SalesReturnDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "SalesReturnApiService.GetAllAsync");
    }

    public async Task<Result<SalesReturnDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesReturnDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "SalesReturnApiService.GetByIdAsync");
    }

    public async Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesReturnDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "SalesReturnApiService.CreateAsync");
    }

    public async Task<Result<SalesReturnDto>> PostAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesReturnDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/post", null, ct),
            "SalesReturnApiService.PostAsync");
    }

    public async Task<Result<SalesReturnDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesReturnDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/cancel", null, ct),
            "SalesReturnApiService.CancelAsync");
    }
}
