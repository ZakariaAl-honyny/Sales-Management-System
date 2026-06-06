using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class UserApiService : ApiServiceBase, IUserApiService
{
    private const string BasePath = "api/v1/users";

    public UserApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<UserDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecuteAsync<List<UserDto>>(
            () => _httpClient.GetAsync($"{BasePath}?includeInactive={includeInactive.ToString().ToLower()}"),
            "UserApiService.GetAllAsync");
    }

    public async Task<Result<UserDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<UserDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}"),
            "UserApiService.GetByIdAsync");
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request)
    {
        return await ExecuteAsync<UserDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request),
            "UserApiService.CreateAsync");
    }

    public async Task<Result<UserDto>> UpdateAsync(int id, UpdateUserRequest request)
    {
        return await ExecuteAsync<UserDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request),
            "UserApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}"),
            "UserApiService.DeleteAsync");
    }

    public async Task<Result> DeletePermanentlyAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/permanent/{id}"),
            "UserApiService.DeletePermanentlyAsync");
    }

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync()
    {
        return await ExecuteAsync<CurrentUserDto>(
            () => _httpClient.GetAsync($"{BasePath}/current"),
            "UserApiService.GetCurrentUserAsync");
    }
}
