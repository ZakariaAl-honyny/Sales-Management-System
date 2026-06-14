using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Party API service implementation
/// </summary>
public class PartyApiService : ApiServiceBase, IPartyApiService
{
    public PartyApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<PartyDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<PartyDto>(
            () => _httpClient.GetAsync($"api/v1/parties?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "PartyApiService.GetAllAsync");
    }

    public async Task<Result<PartyDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<PartyDto>(
            () => _httpClient.GetAsync($"api/v1/parties/{id}"),
            "PartyApiService.GetByIdAsync");
    }

    public async Task<Result<PartyDto>> CreateAsync(CreatePartyRequest request)
    {
        return await ExecuteAsync<PartyDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/parties", request),
            "PartyApiService.CreateAsync");
    }

    public async Task<Result<PartyDto>> UpdateAsync(int id, UpdatePartyRequest request)
    {
        return await ExecuteAsync<PartyDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/parties/{id}", request),
            "PartyApiService.UpdateAsync");
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/parties/{id}"),
            "PartyApiService.DeactivateAsync");
    }
}
