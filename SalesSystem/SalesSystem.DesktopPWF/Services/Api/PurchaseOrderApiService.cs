using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class PurchaseOrderApiService : ApiServiceBase, IPurchaseOrderApiService
{
    private const string BasePath = "api/v1/purchase-orders";

    public PurchaseOrderApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<PurchaseOrderDto>>> GetAllAsync(
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        byte? status = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrEmpty(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (from.HasValue)
            queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)
            queryParams.Add($"to={to.Value:yyyy-MM-dd}");
        if (status.HasValue)
            queryParams.Add($"status={status.Value}");

        var query = string.Join("&", queryParams);
        return await ExecutePagedAsync<PurchaseOrderDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "PurchaseOrderApiService.GetAllAsync");
    }

    public async Task<Result<PurchaseOrderDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseOrderDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "PurchaseOrderApiService.GetByIdAsync");
    }

    public async Task<Result<PurchaseOrderDto>> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseOrderDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "PurchaseOrderApiService.CreateAsync");
    }

    public async Task<Result<PurchaseOrderDto>> UpdateAsync(int id, CreatePurchaseOrderRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseOrderDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "PurchaseOrderApiService.UpdateAsync");
    }

    public async Task<Result<PurchaseOrderDto>> PostAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseOrderDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/post", null, ct),
            "PurchaseOrderApiService.PostAsync");
    }

    public async Task<Result<PurchaseOrderDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseOrderDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/cancel", null, ct),
            "PurchaseOrderApiService.CancelAsync");
    }
}
