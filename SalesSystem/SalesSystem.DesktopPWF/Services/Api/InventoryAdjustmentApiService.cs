using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Inventory Adjustment API service implementation
/// </summary>
public class InventoryAdjustmentApiService : ApiServiceBase, IInventoryAdjustmentApiService
{
    public InventoryAdjustmentApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<InventoryAdjustmentDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<InventoryAdjustmentDto>(
            () => _httpClient.GetAsync($"api/v1/inventory-adjustments?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "InventoryAdjustmentApiService.GetAllAsync");
    }

    public async Task<Result<InventoryAdjustmentDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<InventoryAdjustmentDto>(
            () => _httpClient.GetAsync($"api/v1/inventory-adjustments/{id}"),
            "InventoryAdjustmentApiService.GetByIdAsync");
    }

    public async Task<Result<InventoryAdjustmentDto>> CreateAsync(CreateInventoryAdjustmentRequest request)
    {
        return await ExecuteAsync<InventoryAdjustmentDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/inventory-adjustments", request),
            "InventoryAdjustmentApiService.CreateAsync");
    }

    public async Task<Result<InventoryAdjustmentDto>> PostAsync(int id)
    {
        return await ExecuteAsync<InventoryAdjustmentDto>(
            () => _httpClient.PostAsync($"api/v1/inventory-adjustments/{id}/post", null),
            "InventoryAdjustmentApiService.PostAsync");
    }

    public async Task<Result> CancelAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/inventory-adjustments/{id}/cancel", null),
            "InventoryAdjustmentApiService.CancelAsync");
    }

    public async Task<Result<InventoryAdjustmentDto>> AddLineAsync(int id, AddInventoryAdjustmentLineRequest request)
    {
        return await ExecuteAsync<InventoryAdjustmentDto>(
            () => _httpClient.PostAsJsonAsync($"api/v1/inventory-adjustments/{id}/lines", request),
            "InventoryAdjustmentApiService.AddLineAsync");
    }
}
