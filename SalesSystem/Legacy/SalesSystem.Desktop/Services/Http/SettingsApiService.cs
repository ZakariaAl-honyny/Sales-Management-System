using SalesSystem.Contracts.Common;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class SettingsApiService : ISettingsApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/settings";

    public SettingsApiService(HttpClientService http) => _http = http;

    public async Task<Result<dynamic>> GetSettingsAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<dynamic>(BasePath, ct);
    }

    public async Task<Result> UpdateSettingsAsync(object settings, CancellationToken ct = default)
    {
        return await _http.PutAsync<object>(BasePath, settings, ct);
    }
}

