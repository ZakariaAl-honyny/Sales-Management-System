using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Inventory API service implementation
/// </summary>
public class InventoryApiService : ApiServiceBase, IInventoryApiService
{
    private const string BasePath = "api/v1/inventory";

    public InventoryApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<decimal>> GetStockAsync(int productId, int warehouseId, CancellationToken ct = default)
    {
        return await ExecuteAsync<decimal>(
            () => _httpClient.GetAsync($"{BasePath}/stock?productId={productId}&warehouseId={warehouseId}", ct),
            "InventoryApiService.GetStockAsync");
    }

    public async Task<Result<List<InventoryMovementDto>>> GetMovementsAsync(int? productId = null, int? warehouseId = null, int? movementType = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var url = $"{BasePath}/movements?page={page}&pageSize={pageSize}";
        if (productId.HasValue) url += $"&productId={productId}";
        if (warehouseId.HasValue) url += $"&warehouseId={warehouseId}";
        if (movementType.HasValue) url += $"&movementType={movementType}";

        return await ExecutePagedAsync<InventoryMovementDto>(
            () => _httpClient.GetAsync(url, ct),
            "InventoryApiService.GetMovementsAsync");
    }

    public async Task<Result<List<WarehouseStockDto>>> GetWarehouseStocksAsync(int? warehouseId = null, int? productId = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var url = $"{BasePath}/warehouse-stocks?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue) url += $"&warehouseId={warehouseId}";
        if (productId.HasValue) url += $"&productId={productId}";

        return await ExecutePagedAsync<WarehouseStockDto>(
            () => _httpClient.GetAsync(url, ct),
            "InventoryApiService.GetWarehouseStocksAsync");
    }
}
