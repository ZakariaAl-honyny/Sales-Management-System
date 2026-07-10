using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Customer Receipt API service implementation
/// </summary>
public class CustomerReceiptApiService : ApiServiceBase, ICustomerReceiptApiService
{
    public CustomerReceiptApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<CustomerReceiptDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<CustomerReceiptDto>(
            () => _httpClient.GetAsync($"api/v1/customer-receipts?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "CustomerReceiptApiService.GetAllAsync");
    }

    public async Task<Result<CustomerReceiptDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<CustomerReceiptDto>(
            () => _httpClient.GetAsync($"api/v1/customer-receipts/{id}"),
            "CustomerReceiptApiService.GetByIdAsync");
    }

    public async Task<Result<CustomerReceiptDto>> CreateAsync(CreateCustomerReceiptRequest request)
    {
        return await ExecuteAsync<CustomerReceiptDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/customer-receipts", request),
            "CustomerReceiptApiService.CreateAsync");
    }

    public async Task<Result<CustomerReceiptDto>> UpdateAsync(int id, UpdateCustomerReceiptRequest request)
    {
        return await ExecuteAsync<CustomerReceiptDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/customer-receipts/{id}", request),
            "CustomerReceiptApiService.UpdateAsync");
    }

    public async Task<Result<CustomerReceiptDto>> PostAsync(int id)
    {
        return await ExecuteAsync<CustomerReceiptDto>(
            () => _httpClient.PostAsync($"api/v1/customer-receipts/{id}/post", null),
            "CustomerReceiptApiService.PostAsync");
    }

    public async Task<Result> CancelAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/customer-receipts/{id}/cancel", null),
            "CustomerReceiptApiService.CancelAsync");
    }

    public async Task<Result<CustomerReceiptDto>> AddApplicationAsync(int id, AddReceiptApplicationRequest request)
    {
        return await ExecuteAsync<CustomerReceiptDto>(
            () => _httpClient.PostAsJsonAsync($"api/v1/customer-receipts/{id}/applications", request),
            "CustomerReceiptApiService.AddApplicationAsync");
    }
}
