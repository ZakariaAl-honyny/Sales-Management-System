using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Services.Api;

public sealed class WarehouseApiService : IWarehouseApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/warehouses";

    public WarehouseApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<WarehouseDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var path = $"{BasePath}?includeInactive={includeInactive.ToString().ToLower()}";
        return await _http.GetListAsync<WarehouseDto>(path, ct);
    }

    public async Task<Result<WarehouseDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetAsync<WarehouseDto>($"{BasePath}/{id}", ct);
    }

    public async Task<Result<IReadOnlyList<WarehouseStockDto>>> GetStockAsync(int warehouseId, CancellationToken ct = default)
    {
        return await _http.GetListAsync<WarehouseStockDto>($"{BasePath}/{warehouseId}/stock", ct);
    }

    public async Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<WarehouseDto>(BasePath, r, ct);
    }

    public async Task<Result<WarehouseDto>> UpdateAsync(int id, UpdateWarehouseRequest r, CancellationToken ct = default)
    {
        return await _http.PutAsync<WarehouseDto>($"{BasePath}/{id}", r, ct);
    }

    public async Task<Result> SetDefaultAsync(int id, CancellationToken ct = default)
    {
        return await _http.PutAsync<object>($"{BasePath}/{id}/set-default", null, ct);
    }
}

