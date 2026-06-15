using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Branch API service implementation
/// </summary>
public class BranchApiService : ApiServiceBase, IBranchApiService
{
    public BranchApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<BranchDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<BranchDto>(
            () => _httpClient.GetAsync($"api/v1/branches?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "BranchApiService.GetAllAsync");
    }

    public async Task<Result<BranchDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<BranchDto>(
            () => _httpClient.GetAsync($"api/v1/branches/{id}"),
            "BranchApiService.GetByIdAsync");
    }

    public async Task<Result<BranchDto>> CreateAsync(CreateBranchRequest request)
    {
        return await ExecuteAsync<BranchDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/branches", request),
            "BranchApiService.CreateAsync");
    }

    public async Task<Result<BranchDto>> UpdateAsync(int id, UpdateBranchRequest request)
    {
        return await ExecuteAsync<BranchDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/branches/{id}", request),
            "BranchApiService.UpdateAsync");
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/branches/{id}"),
            "BranchApiService.DeactivateAsync");
    }
}
