using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Purchase Returns
/// </summary>
public class PurchaseReturnApiService : ApiServiceBase, IPurchaseReturnApiService
{
    private const string BasePath = "api/v1/purchase-returns";

    public PurchaseReturnApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<PurchaseReturnDto>>> GetAllAsync(
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
        return await ExecutePagedAsync<PurchaseReturnDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "PurchaseReturnApiService.GetAllAsync");
    }

    public async Task<Result<PurchaseReturnDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseReturnDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "PurchaseReturnApiService.GetByIdAsync");
    }

    public async Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseReturnDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "PurchaseReturnApiService.CreateAsync");
    }

    public async Task<Result<PurchaseReturnDto>> PostAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseReturnDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/post", null, ct),
            "PurchaseReturnApiService.PostAsync");
    }

    public async Task<Result<PurchaseReturnDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseReturnDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/cancel", null, ct),
            "PurchaseReturnApiService.CancelAsync");
    }
}
