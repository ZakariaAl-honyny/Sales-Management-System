using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public class SystemAccountMappingApiService : ApiServiceBase, ISystemAccountMappingApiService
{
    private const string BasePath = "api/v1/system-account-mappings";

    public SystemAccountMappingApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SystemAccountMappingDto>>> GetAllAsync(int? branchId = null, CancellationToken ct = default)
    {
        var url = branchId.HasValue ? $"{BasePath}?branchId={branchId}" : BasePath;
        return await ExecuteAsync<List<SystemAccountMappingDto>>(
            () => _httpClient.GetAsync(url, ct),
            "SystemAccountMappingApiService.GetAllAsync");
    }

    public async Task<Result<SystemAccountMappingDto>> GetByKeyAsync(string key, int? branchId = null, CancellationToken ct = default)
    {
        var url = branchId.HasValue ? $"{BasePath}/by-key/{key}?branchId={branchId}" : $"{BasePath}/by-key/{key}";
        return await ExecuteAsync<SystemAccountMappingDto>(
            () => _httpClient.GetAsync(url, ct),
            "SystemAccountMappingApiService.GetByKeyAsync");
    }

    public async Task<Result<SystemAccountMappingDto>> CreateAsync(CreateSystemAccountMappingRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SystemAccountMappingDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "SystemAccountMappingApiService.CreateAsync");
    }

    public async Task<Result<SystemAccountMappingDto>> UpdateAsync(int id, UpdateSystemAccountMappingRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SystemAccountMappingDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "SystemAccountMappingApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "SystemAccountMappingApiService.DeleteAsync");
    }
}
