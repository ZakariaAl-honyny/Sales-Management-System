using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class BillOfMaterialApiService : ApiServiceBase, IBillOfMaterialApiService
{
    private const string BasePath = "api/v1/assemblies";

    public BillOfMaterialApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<BillOfMaterialDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await ExecutePagedAsync<BillOfMaterialDto>(
            () => _httpClient.GetAsync(BasePath, ct),
            "BillOfMaterialApiService.GetAllAsync");
    }

    public async Task<Result<BillOfMaterialDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<BillOfMaterialDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "BillOfMaterialApiService.GetByIdAsync");
    }

    public async Task<Result<List<BillOfMaterialDto>>> GetByAssemblyAsync(int productId, CancellationToken ct = default)
    {
        return await ExecutePagedAsync<BillOfMaterialDto>(
            () => _httpClient.GetAsync($"{BasePath}/by-product/{productId}", ct),
            "BillOfMaterialApiService.GetByAssemblyAsync");
    }

    public async Task<Result<BillOfMaterialDto>> CreateAsync(CreateBillOfMaterialRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<BillOfMaterialDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "BillOfMaterialApiService.CreateAsync");
    }

    public async Task<Result<BillOfMaterialDto>> UpdateAsync(int id, UpdateBillOfMaterialRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<BillOfMaterialDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "BillOfMaterialApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "BillOfMaterialApiService.DeleteAsync");
    }

    public async Task<Result<ProduceAssemblyResultDto>> ProduceAsync(ProduceAssemblyRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ProduceAssemblyResultDto>(
            () => _httpClient.PostAsJsonAsync($"{BasePath}/produce", request, ct),
            "BillOfMaterialApiService.ProduceAsync");
    }
}
