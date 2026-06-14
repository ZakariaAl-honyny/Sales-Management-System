using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

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

    public async Task<Result<List<InventoryTransactionDto>>> GetMovementsAsync(int? productId = null, int? warehouseId = null, int? movementType = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var url = $"{BasePath}/movements?page={page}&pageSize={pageSize}";
        if (productId.HasValue) url += $"&productId={productId}";
        if (warehouseId.HasValue) url += $"&warehouseId={warehouseId}";
        if (movementType.HasValue) url += $"&movementType={movementType}";

        return await ExecutePagedAsync<InventoryTransactionDto>(
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

    public async Task<Result<InventoryTransactionDto>> CreateInventoryTransactionAsync(CreateInventoryTransactionRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<InventoryTransactionDto>(
            () => _httpClient.PostAsJsonAsync($"{BasePath}/transactions", request, ct),
            "InventoryApiService.CreateInventoryTransactionAsync");
    }

    public async Task<Result<InventoryTransactionDto>> PostInventoryTransactionAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<InventoryTransactionDto>(
            () => _httpClient.PostAsync($"{BasePath}/transactions/{id}/post", null, ct),
            "InventoryApiService.PostInventoryTransactionAsync");
    }

    public async Task<Result<InventoryTransactionDto>> CancelInventoryTransactionAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<InventoryTransactionDto>(
            () => _httpClient.PostAsync($"{BasePath}/transactions/{id}/cancel", null, ct),
            "InventoryApiService.CancelInventoryTransactionAsync");
    }

    public async Task<Result<InventoryTransactionDto>> GetInventoryTransactionByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<InventoryTransactionDto>(
            () => _httpClient.GetAsync($"{BasePath}/transactions/{id}", ct),
            "InventoryApiService.GetInventoryTransactionByIdAsync");
    }
}
