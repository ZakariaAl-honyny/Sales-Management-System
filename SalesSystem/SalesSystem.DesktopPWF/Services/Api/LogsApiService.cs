using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class LogsApiService : ApiServiceBase, ILogsApiService
{
    public LogsApiService(HttpClient httpClient, ISessionService session) 
        : base(httpClient, session)
    {
    }

    public async Task<Result> SendLogAsync(CreateLogRequest request)
    {
        try
        {
            // We don't use ExecuteCommandAsync because it might enter an infinite loop 
            // if HandleConnectionError tries to log back to the API.
            var response = await _httpClient.PostAsJsonAsync("api/v1/logs", request);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure("Failed to send log");
        }
        catch
        {
            // Silent failure for logging to avoid cascading errors
            return Result.Failure("Exception while sending log");
        }
    }

    public async Task<Result<PagedResult<SystemLogDto>>> QueryLogsAsync(
        int? level = null, string? source = null, string? search = null,
        DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (level.HasValue)
            queryParams.Add($"level={level.Value}");
        if (!string.IsNullOrEmpty(source))
            queryParams.Add($"source={Uri.EscapeDataString(source)}");
        if (!string.IsNullOrEmpty(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (from.HasValue)
            queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)
            queryParams.Add($"to={to.Value:yyyy-MM-dd}");

        var query = string.Join("&", queryParams);
        return await ExecuteAsync<PagedResult<SystemLogDto>>(
            () => _httpClient.GetAsync($"api/v1/logs?{query}", ct),
            "LogsApiService.QueryLogsAsync");
    }
}
