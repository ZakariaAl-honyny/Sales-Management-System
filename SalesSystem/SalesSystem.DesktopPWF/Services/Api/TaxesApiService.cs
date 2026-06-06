using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class TaxesApiService : ApiServiceBase, ITaxesApiService
{
    public TaxesApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<TaxDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecuteAsync<List<TaxDto>>(
            () => _httpClient.GetAsync($"api/v1/taxes?includeInactive={includeInactive.ToString().ToLower()}"),
            "TaxesApiService.GetAllAsync");
    }

    public async Task<Result<TaxDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<TaxDto>(
            () => _httpClient.GetAsync($"api/v1/taxes/{id}"),
            "TaxesApiService.GetByIdAsync");
    }

    public async Task<Result<TaxDto>> CreateAsync(CreateTaxRequest request)
    {
        return await ExecuteAsync<TaxDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/taxes", request),
            "TaxesApiService.CreateAsync");
    }

    public async Task<Result<TaxDto>> UpdateAsync(int id, UpdateTaxRequest request)
    {
        return await ExecuteAsync<TaxDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/taxes/{id}", request),
            "TaxesApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/taxes/{id}"),
            "TaxesApiService.DeleteAsync");
    }

    public async Task<Result> DeletePermanentlyAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/taxes/permanent/{id}"),
            "TaxesApiService.DeletePermanentlyAsync");
    }
}
