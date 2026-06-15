using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class CustomerContactApiService : ApiServiceBase, ICustomerContactApiService
{
    public CustomerContactApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<CustomerContactDto>>> GetAllAsync(int customerId)
    {
        return await ExecuteAsync<List<CustomerContactDto>>(
            () => _httpClient.GetAsync($"api/v1/customer-contacts?customerId={customerId}"),
            "CustomerContactApiService.GetAllAsync");
    }

    public async Task<Result<CustomerContactDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<CustomerContactDto>(
            () => _httpClient.GetAsync($"api/v1/customer-contacts/{id}"),
            "CustomerContactApiService.GetByIdAsync");
    }

    public async Task<Result<CustomerContactDto>> CreateAsync(CreateCustomerContactRequest request)
    {
        return await ExecuteAsync<CustomerContactDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/customer-contacts", request),
            "CustomerContactApiService.CreateAsync");
    }

    public async Task<Result<CustomerContactDto>> UpdateAsync(int id, UpdateCustomerContactRequest request)
    {
        return await ExecuteAsync<CustomerContactDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/customer-contacts/{id}", request),
            "CustomerContactApiService.UpdateAsync");
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/customer-contacts/{id}"),
            "CustomerContactApiService.DeactivateAsync");
    }
}
