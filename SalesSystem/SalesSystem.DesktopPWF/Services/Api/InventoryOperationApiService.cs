using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Inventory Operation API service implementation
/// </summary>
public class InventoryOperationApiService : ApiServiceBase, IInventoryOperationApiService
{
    public InventoryOperationApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<InventoryOperationDto>>> GetAllAsync(int? warehouseId = null, byte? operationType = null, int page = 1, int pageSize = 1000)
    {
        var query = $"api/v1/inventory-operations?page={page}&pageSize={pageSize}";
        if (warehouseId.HasValue)
            query += $"&warehouseId={warehouseId.Value}";
        if (operationType.HasValue)
            query += $"&operationType={operationType.Value}";

        return await ExecutePagedAsync<InventoryOperationDto>(
            () => _httpClient.GetAsync(query),
            "InventoryOperationApiService.GetAllAsync");
    }

    public async Task<Result<InventoryOperationDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<InventoryOperationDto>(
            () => _httpClient.GetAsync($"api/v1/inventory-operations/{id}"),
            "InventoryOperationApiService.GetByIdAsync");
    }

    public async Task<Result<InventoryOperationDto>> CreateAsync(CreateInventoryOperationRequest request)
    {
        return await ExecuteAsync<InventoryOperationDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/inventory-operations", request),
            "InventoryOperationApiService.CreateAsync");
    }

    public async Task<Result<InventoryOperationDto>> PostAsync(int id)
    {
        return await ExecuteAsync<InventoryOperationDto>(
            () => _httpClient.PostAsync($"api/v1/inventory-operations/{id}/post", null),
            "InventoryOperationApiService.PostAsync");
    }

    public async Task<Result<InventoryOperationDto>> CancelAsync(int id)
    {
        return await ExecuteAsync<InventoryOperationDto>(
            () => _httpClient.PostAsync($"api/v1/inventory-operations/{id}/cancel", null),
            "InventoryOperationApiService.CancelAsync");
    }
}
