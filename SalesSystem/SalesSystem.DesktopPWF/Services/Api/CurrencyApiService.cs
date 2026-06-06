using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class CurrencyApiService : ApiServiceBase, ICurrencyApiService
{
    public CurrencyApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<CurrencyDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecuteAsync<List<CurrencyDto>>(
            () => _httpClient.GetAsync($"api/v1/currencies?includeInactive={includeInactive}"),
            "CurrencyApiService.GetAllAsync");
    }

    public async Task<Result<CurrencyDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<CurrencyDto>(
            () => _httpClient.GetAsync($"api/v1/currencies/{id}"),
            "CurrencyApiService.GetByIdAsync");
    }

    public async Task<Result<CurrencyDto>> CreateAsync(CreateCurrencyRequest request)
    {
        return await ExecuteAsync<CurrencyDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/currencies", request),
            "CurrencyApiService.CreateAsync");
    }

    public async Task<Result<CurrencyDto>> UpdateAsync(int id, UpdateCurrencyRequest request)
    {
        return await ExecuteAsync<CurrencyDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/currencies/{id}", request),
            "CurrencyApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/currencies/{id}"),
            "CurrencyApiService.DeleteAsync");
    }

    public async Task<Result> DeletePermanentlyAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/currencies/permanent/{id}"),
            "CurrencyApiService.DeletePermanentlyAsync");
    }

    public async Task<Result> UpdateExchangeRateAsync(int id, decimal newRate)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PutAsJsonAsync($"api/v1/currencies/{id}/exchange-rate", new UpdateExchangeRateRequest(newRate)),
            "CurrencyApiService.UpdateExchangeRateAsync");
    }

    public async Task<Result<List<ExchangeRateHistoryDto>>> GetRateHistoryAsync(int currencyId)
    {
        return await ExecuteAsync<List<ExchangeRateHistoryDto>>(
            () => _httpClient.GetAsync($"api/v1/currencies/{currencyId}/history"),
            "CurrencyApiService.GetRateHistoryAsync");
    }
}
