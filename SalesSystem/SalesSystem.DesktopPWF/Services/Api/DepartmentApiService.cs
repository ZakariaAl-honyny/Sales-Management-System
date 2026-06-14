using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Department API service implementation
/// </summary>
public class DepartmentApiService : ApiServiceBase, IDepartmentApiService
{
    public DepartmentApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<DepartmentDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<DepartmentDto>(
            () => _httpClient.GetAsync($"api/v1/departments?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "DepartmentApiService.GetAllAsync");
    }

    public async Task<Result<DepartmentDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<DepartmentDto>(
            () => _httpClient.GetAsync($"api/v1/departments/{id}"),
            "DepartmentApiService.GetByIdAsync");
    }

    public async Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest request)
    {
        return await ExecuteAsync<DepartmentDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/departments", request),
            "DepartmentApiService.CreateAsync");
    }

    public async Task<Result<DepartmentDto>> UpdateAsync(int id, UpdateDepartmentRequest request)
    {
        return await ExecuteAsync<DepartmentDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/departments/{id}", request),
            "DepartmentApiService.UpdateAsync");
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/departments/{id}"),
            "DepartmentApiService.DeactivateAsync");
    }
}
