using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class SettingsApiService : ApiServiceBase, ISettingsApiService
{
    private StoreSettingsDto? _cachedSettings;
    private readonly object _cacheLock = new();

    public SettingsApiService(HttpClient httpClient, ISessionService session) 
        : base(httpClient, session)
    {
    }

    public async Task<Result<StoreSettingsDto>> GetSettingsAsync(CancellationToken ct = default)
    {
        lock (_cacheLock)
        {
            if (_cachedSettings != null)
            {
                return Result<StoreSettingsDto>.Success(_cachedSettings);
            }
        }

        var result = await ExecuteAsync<StoreSettingsDto>(
            () => _httpClient.GetAsync("api/v1/settings", ct),
            "SettingsApiService.GetSettingsAsync");

        if (result.IsSuccess && result.Value != null)
        {
            lock (_cacheLock)
            {
                _cachedSettings = result.Value;
            }
        }

        return result;
    }

    public async Task<Result<StoreSettingsDto>> UpdateSettingsAsync(UpdateSettingsRequest request, CancellationToken ct = default)
    {
        var result = await ExecuteAsync<StoreSettingsDto>(
            () => _httpClient.PutAsJsonAsync("api/v1/settings", request, ct),
            "SettingsApiService.UpdateSettingsAsync");

        if (result.IsSuccess && result.Value != null)
        {
            lock (_cacheLock)
            {
                _cachedSettings = result.Value;
            }
        }

        return result;
    }

    public async Task<Result<PrintSettingsDto>> GetPrintSettingsAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<PrintSettingsDto>(
            () => _httpClient.GetAsync("api/v1/settings/print", ct),
            "SettingsApiService.GetPrintSettingsAsync");
    }

    public async Task<Result> UpdatePrintSettingsAsync(UpdatePrintSettingsRequest request, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PutAsJsonAsync("api/v1/settings/print", request, ct),
            "SettingsApiService.UpdatePrintSettingsAsync");
    }

    public async Task<Result<int>> GetCostingMethodAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<int>(
            () => _httpClient.GetAsync("api/v1/settings/costing-method", ct),
            "SettingsApiService.GetCostingMethodAsync");
    }

    public async Task<Result> SetCostingMethodAsync(UpdateCostingMethodRequest request, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PutAsJsonAsync("api/v1/settings/costing-method", request, ct),
            "SettingsApiService.SetCostingMethodAsync");
    }

    public async Task<Result<Dictionary<string, string>>> GetAllSystemSettingsAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<Dictionary<string, string>>(
            () => _httpClient.GetAsync("api/v1/settings/system", ct),
            "SettingsApiService.GetAllSystemSettingsAsync");
    }

    public async Task<Result> UpdateSystemSettingsAsync(Dictionary<string, string> settings, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PutAsJsonAsync("api/v1/settings/system", settings, ct),
            "SettingsApiService.UpdateSystemSettingsAsync");
    }

    public void RefreshCache()
    {
        lock (_cacheLock)
        {
            _cachedSettings = null;
        }
    }
}
