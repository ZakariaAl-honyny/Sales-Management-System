using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class UnitApiService : ApiServiceBase, IUnitApiService
{
    public UnitApiService(HttpClient httpClient, ISessionService session) 
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<UnitDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<UnitDto>(
            () => _httpClient.GetAsync($"api/v1/units?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "UnitApiService.GetAllAsync");
    }

    public async Task<Result<UnitDto>> CreateAsync(CreateUnitRequest request)
    {
        return await ExecuteAsync<UnitDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/units", request),
            "UnitApiService.CreateAsync");
    }

    public async Task<Result<UnitDto>> UpdateAsync(int id, UpdateUnitRequest request)
    {
        return await ExecuteAsync<UnitDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/units/{id}", request),
            "UnitApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/units/{id}"),
            "UnitApiService.DeleteAsync");
    }

    public async Task<Result> DeletePermanentlyAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/units/{id}/permanent"),
            "UnitApiService.DeletePermanentlyAsync");
    }
}
