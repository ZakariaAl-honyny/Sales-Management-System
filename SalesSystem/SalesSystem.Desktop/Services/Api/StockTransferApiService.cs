using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Services.Api;

public sealed class StockTransferApiService : IStockTransferApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/stock-transfers";

    public StockTransferApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<StockTransferDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrEmpty(search)) query["search"] = search;
        if (from.HasValue) query["from"] = from.Value.ToString("yyyy-MM-dd");
        if (to.HasValue) query["to"] = to.Value.ToString("yyyy-MM-dd");

        return await _http.GetListAsync<StockTransferDto>($"{BasePath}?{query}", ct);
    }

    public async Task<Result<StockTransferDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetAsync<StockTransferDto>($"{BasePath}/{id}", ct);
    }

    public async Task<Result<StockTransferDto>> CreateAsync(CreateStockTransferRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<StockTransferDto>(BasePath, r, ct);
    }

    public async Task<Result> PostAsync(int id, CancellationToken ct = default)
    {
        return await _http.PostAsync<object>($"{BasePath}/{id}/post", new { }, ct);
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct = default)
    {
        return await _http.PostAsync<object>($"{BasePath}/{id}/cancel", new { }, ct);
    }
}
