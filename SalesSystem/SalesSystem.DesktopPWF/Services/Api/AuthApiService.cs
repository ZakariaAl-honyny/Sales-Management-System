using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public class AuthApiService : ApiServiceBase, IAuthApiService
{
    public AuthApiService(HttpClient httpClient, ISessionService session) 
        : base(httpClient, session)
    {
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request);
            return await HandleResponseAsync<LoginResponse>(response);
        }
        catch (Exception ex)
        {
            return HandleConnectionError<LoginResponse>(ex, "AuthApiService.LoginAsync");
        }
    }
}
