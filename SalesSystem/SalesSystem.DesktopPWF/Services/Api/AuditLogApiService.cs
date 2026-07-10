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
            var queryParams = new List<string>();
            if (query.UserId.HasValue) queryParams.Add($"userId={query.UserId}");
            if (!string.IsNullOrEmpty(query.Action)) queryParams.Add($"action={Uri.EscapeDataString(query.Action)}");
            if (!string.IsNullOrEmpty(query.EntityType)) queryParams.Add($"entityType={Uri.EscapeDataString(query.EntityType)}");
            if (query.From.HasValue) queryParams.Add($"from={query.From.Value:yyyy-MM-dd}");
            if (query.To.HasValue) queryParams.Add($"to={query.To.Value:yyyy-MM-dd}");
            queryParams.Add($"page={query.Page}");
            queryParams.Add($"pageSize={query.PageSize}");

            var url = $"{BasePath}?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url);
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
            ? $"{BasePath}/login-history?userId={userId.Value}&limit={limit}"
            : $"{BasePath}/login-history?limit={limit}";
        return await ExecuteAsync<List<AuditLogDto>>(
            () => _httpClient.GetAsync(url),
            "AuditLogApiService.GetLoginHistoryAsync");
    }
}
