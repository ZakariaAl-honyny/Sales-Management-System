using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class AuditLogApiService : ApiServiceBase, IAuditLogApiService
{
    private const string BasePath = "api/v1/audit-logs";

    public AuditLogApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<PagedResult<AuditLogDto>>> QueryAsync(AuditLogQuery query)
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.PostAsJsonAsync($"{BasePath}/query", query);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>();
                return Result<PagedResult<AuditLogDto>>.Success(data!);
            }
            return await HandleResponseAsync<PagedResult<AuditLogDto>>(response);
        }
        catch (Exception ex)
        {
            return HandleConnectionError<PagedResult<AuditLogDto>>(ex, "AuditLogApiService.QueryAsync");
        }
    }

    public async Task<Result<List<AuditLogDto>>> GetUserHistoryAsync(int userId, int limit = 50)
    {
        return await ExecuteAsync<List<AuditLogDto>>(
            () => _httpClient.GetAsync($"{BasePath}/user/{userId}?limit={limit}"),
            "AuditLogApiService.GetUserHistoryAsync");
    }

    public async Task<Result<List<AuditLogDto>>> GetLoginHistoryAsync(int? userId, int limit = 50)
    {
        var url = userId.HasValue
            ? $"{BasePath}/login?userId={userId.Value}&limit={limit}"
            : $"{BasePath}/login?limit={limit}";
        return await ExecuteAsync<List<AuditLogDto>>(
            () => _httpClient.GetAsync(url),
            "AuditLogApiService.GetLoginHistoryAsync");
    }
}
