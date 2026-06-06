using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public class PermissionApiService : ApiServiceBase, IPermissionApiService
{
    private const string BasePath = "api/v1/permissions";

    public PermissionApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<PermissionDto>>> GetAllAsync()
    {
        return await ExecuteAsync<List<PermissionDto>>(
            () => _httpClient.GetAsync(BasePath),
            "PermissionApiService.GetAllAsync");
    }

    public async Task<Result<Dictionary<byte, List<int>>>> GetRolePermissionsAsync()
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.GetAsync($"{BasePath}/roles");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<Dictionary<byte, List<int>>>();
                return Result<Dictionary<byte, List<int>>>.Success(data!);
            }
            return await HandleResponseAsync<Dictionary<byte, List<int>>>(response);
        }
        catch (Exception ex)
        {
            return HandleConnectionError<Dictionary<byte, List<int>>>(ex, "PermissionApiService.GetRolePermissionsAsync");
        }
    }

    public async Task<Result> UpdateRolePermissionsAsync(byte role, List<int> permissionIds)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/roles/{role}", permissionIds),
            "PermissionApiService.UpdateRolePermissionsAsync");
    }
}
