using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class RoleApiService : ApiServiceBase, IRoleApiService
{
    private const string BasePath = "api/v1/roles";

    public RoleApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<RoleDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<RoleDto>>(
            () => _httpClient.GetAsync($"{BasePath}?includeInactive={includeInactive.ToString().ToLower()}", ct),
            "RoleApiService.GetAllAsync");
    }

    public async Task<Result<RoleDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<RoleDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "RoleApiService.GetByIdAsync");
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<RoleDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "RoleApiService.CreateAsync");
    }

    public async Task<Result<RoleDto>> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<RoleDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "RoleApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "RoleApiService.DeleteAsync");
    }
}
