using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Warehouse Transfers
/// </summary>
public class WarehouseTransferApiService : ApiServiceBase, IWarehouseTransferApiService
{
    private const string BasePath = "api/v1/warehouse-transfers";

    public WarehouseTransferApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<WarehouseTransferDto>>> GetAllAsync(
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
        return await ExecutePagedAsync<WarehouseTransferDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "WarehouseTransferApiService.GetAllAsync");
    }

    public async Task<Result<WarehouseTransferDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<WarehouseTransferDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "WarehouseTransferApiService.GetByIdAsync");
    }

    public async Task<Result<WarehouseTransferDto>> CreateAsync(CreateWarehouseTransferRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<WarehouseTransferDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "WarehouseTransferApiService.CreateAsync");
    }

    public async Task<Result<WarehouseTransferDto>> UpdateAsync(int id, CreateWarehouseTransferRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<WarehouseTransferDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "WarehouseTransferApiService.UpdateAsync");
    }

    public async Task<Result<WarehouseTransferDto>> PostAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<WarehouseTransferDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/post", null, ct),
            "WarehouseTransferApiService.PostAsync");
    }

    public async Task<Result<WarehouseTransferDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<WarehouseTransferDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/cancel", null, ct),
            "WarehouseTransferApiService.CancelAsync");
    }
}
