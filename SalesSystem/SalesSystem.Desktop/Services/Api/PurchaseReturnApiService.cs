using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Services.Api;

public sealed class PurchaseReturnApiService : IPurchaseReturnApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/purchase-returns";

    public PurchaseReturnApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<PurchaseReturnDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, int? supplierId = null, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrEmpty(search)) query["search"] = search;
        if (from.HasValue) query["from"] = from.Value.ToString("yyyy-MM-dd");
        if (to.HasValue) query["to"] = to.Value.ToString("yyyy-MM-dd");
        if (supplierId.HasValue) query["supplierId"] = supplierId.Value.ToString();

        return await _http.GetListAsync<PurchaseReturnDto>($"{BasePath}?{query}", ct);
    }

    public async Task<Result<PurchaseReturnDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetAsync<PurchaseReturnDto>($"{BasePath}/{id}", ct);
    }

    public async Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<PurchaseReturnDto>(BasePath, r, ct);
    }

    public async Task<Result> PostAsync(int id, CancellationToken ct = default)
    {
        return await _http.PostAsync($"{BasePath}/{id}/post", null, ct);
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct = default)
    {
        return await _http.PostAsync($"{BasePath}/{id}/cancel", null, ct);
    }
}
