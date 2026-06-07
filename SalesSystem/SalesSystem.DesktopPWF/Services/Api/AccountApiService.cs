using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class AccountApiService : ApiServiceBase, IAccountApiService
{
    public AccountApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<AccountDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<List<AccountDto>>(
            () => _httpClient.GetAsync("api/v1/accounts", ct),
            "AccountApiService.GetAllAsync");
    }

    public async Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<List<AccountTreeNodeDto>>(
            () => _httpClient.GetAsync("api/v1/accounts/tree", ct),
            "AccountApiService.GetTreeAsync");
    }

    public async Task<Result<AccountDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<AccountDto>(
            () => _httpClient.GetAsync($"api/v1/accounts/{id}", ct),
            "AccountApiService.GetByIdAsync");
    }

    public async Task<Result<List<AccountDto>>> GetByTypeAsync(byte type, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<AccountDto>>(
            () => _httpClient.GetAsync($"api/v1/accounts/by-type/{type}", ct),
            "AccountApiService.GetByTypeAsync");
    }

    public async Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<AccountDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/accounts", request, ct),
            "AccountApiService.CreateAsync");
    }

    public async Task<Result<AccountDto>> UpdateAsync(int id, UpdateAccountRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<AccountDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/accounts/{id}", request, ct),
            "AccountApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/accounts/{id}", ct),
            "AccountApiService.DeleteAsync");
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/accounts/permanent/{id}", ct),
            "AccountApiService.PermanentDeleteAsync");
    }
}
