using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public class UserSessionApiService : ApiServiceBase, IUserSessionApiService
{
    private const string BasePath = "api/v1/sessions";

    public UserSessionApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<UserSessionDto>>> GetAllAsync(int? userId = null, bool includeRevoked = false, CancellationToken ct = default)
    {
        var url = $"{BasePath}?includeRevoked={includeRevoked.ToString().ToLower()}";
        if (userId.HasValue)
            url += $"&userId={userId.Value}";

        return await ExecuteAsync<List<UserSessionDto>>(
            () => _httpClient.GetAsync(url, ct),
            "UserSessionApiService.GetAllAsync");
    }

    public async Task<Result> RevokeAsync(long sessionId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"{BasePath}/{sessionId}/revoke", null, ct),
            "UserSessionApiService.RevokeAsync");
    }
}
