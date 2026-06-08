using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public class InventoryBatchApiService : ApiServiceBase, IInventoryBatchApiService
{
    private const string BasePath = "api/v1/inventory-batches";

    public InventoryBatchApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<InventoryBatchDto>>> GetByProductAsync(int productId, int? warehouseId, CancellationToken ct = default)
    {
        var url = $"{BasePath}?productId={productId}";
        if (warehouseId.HasValue)
            url += $"&warehouseId={warehouseId.Value}";

        return await ExecuteAsync<List<InventoryBatchDto>>(
            () => _httpClient.GetAsync(url, ct),
            "InventoryBatchApiService.GetByProductAsync");
    }

    public async Task<Result<InventoryBatchDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<InventoryBatchDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "InventoryBatchApiService.GetByIdAsync");
    }

    public async Task<Result<InventoryBatchDto>> CreateAsync(CreateInventoryBatchRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<InventoryBatchDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "InventoryBatchApiService.CreateAsync");
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "InventoryBatchApiService.DeactivateAsync");
    }
}
