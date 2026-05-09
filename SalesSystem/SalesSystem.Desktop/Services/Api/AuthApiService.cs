using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Services.Api;

public sealed class AuthApiService : IAuthApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/auth";

    public AuthApiService(HttpClientService http) => _http = http;

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        return await _http.PostAsync<LoginResponse>($"{BasePath}/login", request, ct);
    }
}

