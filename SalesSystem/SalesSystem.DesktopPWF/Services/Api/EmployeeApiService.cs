using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Employee API service implementation
/// </summary>
public class EmployeeApiService : ApiServiceBase, IEmployeeApiService
{
    public EmployeeApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<EmployeeDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<EmployeeDto>(
            () => _httpClient.GetAsync($"api/v1/employees?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "EmployeeApiService.GetAllAsync");
    }

    public async Task<Result<EmployeeDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<EmployeeDto>(
            () => _httpClient.GetAsync($"api/v1/employees/{id}"),
            "EmployeeApiService.GetByIdAsync");
    }

    public async Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeRequest request)
    {
        return await ExecuteAsync<EmployeeDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/employees", request),
            "EmployeeApiService.CreateAsync");
    }

    public async Task<Result<EmployeeDto>> UpdateAsync(int id, UpdateEmployeeRequest request)
    {
        return await ExecuteAsync<EmployeeDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/employees/{id}", request),
            "EmployeeApiService.UpdateAsync");
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/employees/{id}"),
            "EmployeeApiService.DeactivateAsync");
    }
}
