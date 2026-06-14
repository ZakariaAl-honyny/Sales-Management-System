using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public class AccountCategoryApiService : ApiServiceBase, IAccountCategoryApiService
{
    private const string BasePath = "api/v1/account-categories";

    public AccountCategoryApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<AccountCategoryDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<List<AccountCategoryDto>>(
            () => _httpClient.GetAsync(BasePath, ct),
            "AccountCategoryApiService.GetAllAsync");
    }

    public async Task<Result<AccountCategoryDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<AccountCategoryDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "AccountCategoryApiService.GetByIdAsync");
    }

    public async Task<Result<AccountCategoryDto>> CreateAsync(CreateAccountCategoryRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<AccountCategoryDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "AccountCategoryApiService.CreateAsync");
    }

    public async Task<Result<AccountCategoryDto>> UpdateAsync(int id, UpdateAccountCategoryRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<AccountCategoryDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "AccountCategoryApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "AccountCategoryApiService.DeleteAsync");
    }
}
