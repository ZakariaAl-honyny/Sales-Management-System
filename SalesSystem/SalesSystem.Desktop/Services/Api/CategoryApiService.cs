using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Services.Api;

public sealed class CategoryApiService : ICategoryApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/categories";

    public CategoryApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var path = $"{BasePath}?includeInactive={includeInactive.ToString().ToLower()}";
        return await _http.GetListAsync<CategoryDto>(path, ct);
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<CategoryDto>(BasePath, r, ct);
    }

    public async Task<Result<CategoryDto>> UpdateAsync(int id, UpdateCategoryRequest r, CancellationToken ct = default)
    {
        return await _http.PutAsync<CategoryDto>($"{BasePath}/{id}", r, ct);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await _http.DeleteAsync($"{BasePath}/{id}", ct);
    }
}

