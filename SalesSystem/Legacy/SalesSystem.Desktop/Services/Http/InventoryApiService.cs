using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class InventoryApiService : IInventoryApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/inventory";

    public InventoryApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<WarehouseStockDto>>> GetStockByWarehouseAsync(int warehouseId, CancellationToken ct = default)
    {
        return await _http.GetListAsync<WarehouseStockDto>($"{BasePath}/warehouse/{warehouseId}", ct);
    }

    public async Task<Result<IReadOnlyList<WarehouseStockDto>>> GetStockByProductAsync(int productId, CancellationToken ct = default)
    {
        return await _http.GetListAsync<WarehouseStockDto>($"{BasePath}/product/{productId}", ct);
    }
}

