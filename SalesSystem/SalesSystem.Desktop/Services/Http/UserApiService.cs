using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class UserApiService : IUserApiService
{
    private readonly HttpClient _httpClient;
    public UserApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<UserDto>>("api/v1/users", ct);
            return Result<IReadOnlyList<UserDto>>.Success(response ?? new List<UserDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<UserDto>>.Failure(ex.Message); }
    }
}
