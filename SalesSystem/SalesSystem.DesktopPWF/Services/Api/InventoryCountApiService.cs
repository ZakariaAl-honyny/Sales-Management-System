using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Inventory Count API service implementation
/// </summary>
public class InventoryCountApiService : ApiServiceBase, IInventoryCountApiService
{
    public InventoryCountApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<InventoryCountDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<InventoryCountDto>(
            () => _httpClient.GetAsync($"api/v1/inventory-counts?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "InventoryCountApiService.GetAllAsync");
    }

    public async Task<Result<InventoryCountDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<InventoryCountDto>(
            () => _httpClient.GetAsync($"api/v1/inventory-counts/{id}"),
            "InventoryCountApiService.GetByIdAsync");
    }

    public async Task<Result<InventoryCountDto>> CreateAsync(CreateInventoryCountRequest request)
    {
        return await ExecuteAsync<InventoryCountDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/inventory-counts", request),
            "InventoryCountApiService.CreateAsync");
    }

    public async Task<Result<InventoryCountDto>> PostAsync(int id)
    {
        return await ExecuteAsync<InventoryCountDto>(
            () => _httpClient.PostAsync($"api/v1/inventory-counts/{id}/post", null),
            "InventoryCountApiService.PostAsync");
    }

    public async Task<Result> CancelAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/inventory-counts/{id}/cancel", null),
            "InventoryCountApiService.CancelAsync");
    }

    public async Task<Result<InventoryCountDto>> AddLineAsync(int id, AddInventoryCountLineRequest request)
    {
        return await ExecuteAsync<InventoryCountDto>(
            () => _httpClient.PostAsJsonAsync($"api/v1/inventory-counts/{id}/lines", request),
            "InventoryCountApiService.AddLineAsync");
    }
}
