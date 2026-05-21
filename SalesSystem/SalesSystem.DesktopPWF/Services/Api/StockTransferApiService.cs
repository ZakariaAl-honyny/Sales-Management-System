using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Stock Transfers
/// </summary>
public class StockTransferApiService : ApiServiceBase, IStockTransferApiService
{
    private const string BasePath = "api/v1/stock-transfers";

    public StockTransferApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<StockTransferDto>>> GetAllAsync(
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        byte? status = null,
        bool includeInactive = false,
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
        if (includeInactive)
            queryParams.Add($"includeInactive=true");

        var query = string.Join("&", queryParams);
        return await ExecutePagedAsync<StockTransferDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "StockTransferApiService.GetAllAsync");
    }

    public async Task<Result<StockTransferDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<StockTransferDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "StockTransferApiService.GetByIdAsync");
    }

    public async Task<Result<StockTransferDto>> CreateAsync(CreateStockTransferRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<StockTransferDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "StockTransferApiService.CreateAsync");
    }

    public async Task<Result<StockTransferDto>> UpdateAsync(int id, UpdateStockTransferRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<StockTransferDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "StockTransferApiService.UpdateAsync");
    }

    public async Task<Result<StockTransferDto>> PostAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<StockTransferDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/post", null, ct),
            "StockTransferApiService.PostAsync");
    }

    public async Task<Result<StockTransferDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<StockTransferDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/cancel", null, ct),
            "StockTransferApiService.CancelAsync");
    }
}
