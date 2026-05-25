using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for inventory write-off operations
/// </summary>
public class InventoryWriteOffApiService : ApiServiceBase, IInventoryWriteOffApiService
{
    private const string BasePath = "api/v1/inventory";

    public InventoryWriteOffApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<StockWriteOffDto>> WriteOffAsync(CreateStockWriteOffRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<StockWriteOffDto>(
            () => _httpClient.PostAsJsonAsync($"{BasePath}/writeoff", request, ct),
            "InventoryWriteOffApiService.WriteOffAsync");
    }
}
