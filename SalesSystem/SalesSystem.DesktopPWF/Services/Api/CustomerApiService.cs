using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Customer API service implementation
/// </summary>
public class CustomerApiService : ApiServiceBase, ICustomerApiService
{
    public CustomerApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<CustomerDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<CustomerDto>(
            () => _httpClient.GetAsync($"api/v1/customers?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "CustomerApiService.GetAllAsync");
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<CustomerDto>(
            () => _httpClient.GetAsync($"api/v1/customers/{id}"),
            "CustomerApiService.GetByIdAsync");
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request)
    {
        return await ExecuteAsync<CustomerDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/customers", request),
            "CustomerApiService.CreateAsync");
    }

    public async Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        return await ExecuteAsync<CustomerDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/customers/{id}", request),
            "CustomerApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/customers/{id}"),
            "CustomerApiService.DeleteAsync");
    }

    public async Task<Result> DeletePermanentlyAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/customers/permanent/{id}"),
            "CustomerApiService.DeletePermanentlyAsync");
    }

}
