using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Customer Group API service implementation.
/// Communicates with the dedicated CustomerGroupsController at api/v1/customer-groups.
/// </summary>
public class CustomerGroupApiService : ApiServiceBase, ICustomerGroupApiService
{
    public CustomerGroupApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<CustomerGroupDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<List<CustomerGroupDto>>(
            () => _httpClient.GetAsync("api/v1/customer-groups", ct),
            "CustomerGroupApiService.GetAllAsync");
    }

    public async Task<Result<CustomerGroupDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<CustomerGroupDto>(
            () => _httpClient.GetAsync($"api/v1/customer-groups/{id}", ct),
            "CustomerGroupApiService.GetByIdAsync");
    }

    public async Task<Result<CustomerGroupDto>> CreateAsync(CreateCustomerGroupRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<CustomerGroupDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/customer-groups", request, ct),
            "CustomerGroupApiService.CreateAsync");
    }

    public async Task<Result<CustomerGroupDto>> UpdateAsync(int id, UpdateCustomerGroupRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<CustomerGroupDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/customer-groups/{id}", request, ct),
            "CustomerGroupApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/customer-groups/{id}", ct),
            "CustomerGroupApiService.DeleteAsync");
    }
}
