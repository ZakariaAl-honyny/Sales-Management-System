using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
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
}
