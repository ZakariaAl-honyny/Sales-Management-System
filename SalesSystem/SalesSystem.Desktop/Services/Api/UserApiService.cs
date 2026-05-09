using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Services.Api;

public sealed class UserApiService : IUserApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/users";

    public UserApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await _http.GetListAsync<UserDto>(BasePath, ct);
    }
}

