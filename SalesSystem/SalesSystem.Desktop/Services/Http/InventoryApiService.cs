using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class InventoryApiService : IInventoryApiService
{
    private readonly HttpClient _httpClient;
    public InventoryApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<InventoryMovementDto>>> GetMovementsAsync(string? search = null, int? productId = null, int? warehouseId = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            if (productId.HasValue) query["productId"] = productId.Value.ToString();
            if (warehouseId.HasValue) query["warehouseId"] = warehouseId.Value.ToString();
            
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<InventoryMovementDto>>($"api/v1/inventory/movements?{query}", ct);
            return Result<IReadOnlyList<InventoryMovementDto>>.Success(response ?? new List<InventoryMovementDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<InventoryMovementDto>>.Failure(ex.Message); }
    }

    public async Task<Result<IReadOnlyList<WarehouseStockDto>>> GetStocksAsync(int? warehouseId = null, int? productId = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (warehouseId.HasValue) query["warehouseId"] = warehouseId.Value.ToString();
            if (productId.HasValue) query["productId"] = productId.Value.ToString();
            
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<WarehouseStockDto>>($"api/v1/inventory/stocks?{query}", ct);
            return Result<IReadOnlyList<WarehouseStockDto>>.Success(response ?? new List<WarehouseStockDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<WarehouseStockDto>>.Failure(ex.Message); }
    }
}
