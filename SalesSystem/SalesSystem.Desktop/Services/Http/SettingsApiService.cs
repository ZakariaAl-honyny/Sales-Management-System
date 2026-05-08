using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class SettingsApiService : ISettingsApiService
{
    private readonly HttpClient _httpClient;
    public SettingsApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<StoreSettingsDto>> GetSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<StoreSettingsDto>("api/v1/settings", ct);
            return Result<StoreSettingsDto>.Success(response!);
        }
        catch (Exception ex) { return Result<StoreSettingsDto>.Failure(ex.Message); }
    }

    public async Task<Result<bool>> UpdateSettingsAsync(StoreSettingsDto settings, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/v1/settings", settings, ct);
            return response.IsSuccessStatusCode ? Result<bool>.Success(true) : Result<bool>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<bool>.Failure(ex.Message); }
    }
}
