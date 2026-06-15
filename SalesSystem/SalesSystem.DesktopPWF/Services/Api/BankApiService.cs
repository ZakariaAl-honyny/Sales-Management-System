using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Bank API service implementation
/// </summary>
public class BankApiService : ApiServiceBase, IBankApiService
{
    public BankApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<BankDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<BankDto>(
            () => _httpClient.GetAsync($"api/v1/banks?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "BankApiService.GetAllAsync");
    }

    public async Task<Result<BankDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<BankDto>(
            () => _httpClient.GetAsync($"api/v1/banks/{id}"),
            "BankApiService.GetByIdAsync");
    }

    public async Task<Result<BankDto>> CreateAsync(CreateBankRequest request)
    {
        return await ExecuteAsync<BankDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/banks", request),
            "BankApiService.CreateAsync");
    }

    public async Task<Result<BankDto>> UpdateAsync(int id, UpdateBankRequest request)
    {
        return await ExecuteAsync<BankDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/banks/{id}", request),
            "BankApiService.UpdateAsync");
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/banks/{id}"),
            "BankApiService.DeactivateAsync");
    }
}
